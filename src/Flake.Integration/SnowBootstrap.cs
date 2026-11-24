using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Flake.Integration;

internal enum SnowIntensity
{
    Off,
    Light,
    Steady,
    Heavy,
    Variable
}

/// <summary>
/// User-editable snowfall options stored in %AppData%\Flake\snow-settings.json.
/// Values are clamped so malformed or extreme settings cannot create an excessive particle load.
/// </summary>
internal sealed class SnowSettings
{
    public SnowIntensity Intensity { get; set; } = SnowIntensity.Steady;
    public double Speed { get; set; } = 1.0;
    public double Wind { get; set; } = 0.35;

    public static SnowSettings Load()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Flake");
        string path = Path.Combine(directory, "snow-settings.json");

        try
        {
            Directory.CreateDirectory(directory);
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<SnowSettings>(File.ReadAllText(path));
                if (loaded is not null)
                {
                    loaded.Speed = Math.Clamp(loaded.Speed, 0.1, 3.0);
                    loaded.Wind = Math.Clamp(loaded.Wind, -2.0, 2.0);
                    return loaded;
                }
            }
        }
        catch
        {
            // Invalid settings fall back to safe defaults; the desktop app must keep starting.
        }

        var defaults = new SnowSettings();
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch
        {
            // Settings are optional. Read-only profiles still get default snowfall.
        }

        return defaults;
    }

    public int GetParticleCount()
    {
        return Intensity switch
        {
            SnowIntensity.Off => 0,
            SnowIntensity.Light => 35,
            SnowIntensity.Heavy => 180,
            SnowIntensity.Variable => Random.Shared.Next(45, 151),
            _ => 95
        };
    }
}

/// <summary>
/// Early Flake integration prototype. Creates one transparent snowfall layer per display
/// when Atoll's main window is loaded. The overlays are isolated from Atoll's UI.
/// </summary>
internal static class SnowBootstrap
{
    private static bool _started;
    private static readonly List<SnowOverlay> Overlays = [];

    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded),
            handledEventsToo: true);
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_started || sender is not Window window || window.GetType().FullName != "DynamicIsland.MainWindow")
            return;

        _started = true;
        window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            try
            {
                var settings = SnowSettings.Load();
                if (settings.Intensity == SnowIntensity.Off)
                    return;

                foreach (var screen in Forms.Screen.AllScreens)
                {
                    var overlay = new SnowOverlay(screen.Bounds, settings);
                    Overlays.Add(overlay);
                    overlay.Show();
                }
            }
            catch
            {
                // Snow is an optional visual layer: it must never prevent Atoll from starting.
                foreach (var overlay in Overlays)
                    overlay.Close();
                Overlays.Clear();
            }
        }));
    }
}

internal sealed class SnowOverlay : Window
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private readonly Canvas _canvas = new();
    private readonly List<Flake> _flakes = [];
    private readonly Random _random = new();
    private readonly SnowSettings _settings;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private double _elapsedSeconds;

    public SnowOverlay(System.Drawing.Rectangle bounds, SnowSettings settings)
    {
        _settings = settings;
        Title = "Flake Snow";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        IsHitTestVisible = false;
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        Content = _canvas;

        for (int i = 0; i < settings.GetParticleCount(); i++)
            AddFlake(randomY: true);

        SourceInitialized += (_, _) => MakeClickThrough();
        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
        _timer.Tick += (_, _) => Advance();
    }

    private void MakeClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(hwnd, -20);
        SetWindowLong(hwnd, -20, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
    }

    private void AddFlake(bool randomY)
    {
        double size = 1.2 + _random.NextDouble() * 4.8;
        var shape = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromArgb((byte)(100 + _random.Next(110)), 255, 255, 255)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(shape, _random.NextDouble() * Math.Max(1, ActualWidth > 0 ? ActualWidth : Width));
        Canvas.SetTop(shape, randomY ? _random.NextDouble() * Math.Max(1, ActualHeight > 0 ? ActualHeight : Height) : -10);
        _canvas.Children.Add(shape);

        double speed = (0.35 + _random.NextDouble() * 1.65) * _settings.Speed;
        double drift = (_random.NextDouble() - 0.5) * 0.45;
        _flakes.Add(new Flake(shape, speed, drift, _random.NextDouble() * Math.PI * 2));
    }

    private void Advance()
    {
        _elapsedSeconds += _timer.Interval.TotalSeconds;
        double width = ActualWidth > 0 ? ActualWidth : Width;
        double height = ActualHeight > 0 ? ActualHeight : Height;
        double gust = Math.Sin(_elapsedSeconds * 0.42) * Math.Abs(_settings.Wind) * 0.65;
        double wind = _settings.Wind + gust;

        foreach (var flake in _flakes)
        {
            double x = Canvas.GetLeft(flake.Shape) + flake.Drift + wind * 0.035;
            double sway = Math.Sin(_elapsedSeconds * 1.7 + flake.Phase) * 0.18;
            double y = Canvas.GetTop(flake.Shape) + flake.Speed;
            x += sway;

            if (y > height + 8)
            {
                y = -10;
                x = _random.NextDouble() * width;
            }
            if (x < -8) x = width + 4;
            if (x > width + 8) x = -4;
            Canvas.SetLeft(flake.Shape, x);
            Canvas.SetTop(flake.Shape, y);
        }
    }

    private sealed record Flake(Ellipse Shape, double Speed, double Drift, double Phase);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
}
