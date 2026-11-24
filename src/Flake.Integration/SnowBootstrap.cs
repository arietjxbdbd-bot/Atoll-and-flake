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
        string path = GetSettingsPath();
        string directory = System.IO.Path.GetDirectoryName(path)!;

        try
        {
            Directory.CreateDirectory(directory);
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<SnowSettings>(File.ReadAllText(path));
                if (loaded is not null)
                {
                    loaded.Speed = double.IsFinite(loaded.Speed) ? Math.Clamp(loaded.Speed, 0.1, 3.0) : 1.0;
                    loaded.Wind = double.IsFinite(loaded.Wind) ? Math.Clamp(loaded.Wind, -2.0, 2.0) : 0.35;
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

    public static string GetSettingsPath()
    {
        string directory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Flake");
        return System.IO.Path.Combine(directory, "snow-settings.json");
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
    private static FileSystemWatcher? _settingsWatcher;
    private static DispatcherTimer? _settingsReloadTimer;

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
                StartSettingsWatcher(window.Dispatcher);

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

    private static void StartSettingsWatcher(Dispatcher dispatcher)
    {
        string path = SnowSettings.GetSettingsPath();
        string directory = System.IO.Path.GetDirectoryName(path)!;
        string fileName = System.IO.Path.GetFileName(path);

        _settingsReloadTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(300), DispatcherPriority.Background, (_, _) =>
            {
                _settingsReloadTimer?.Stop();
                try
                {
                    var updated = SnowSettings.Load();
                    foreach (var overlay in Overlays)
                        overlay.UpdateSettings(updated);
                }
                catch
                {
                    // Keep the last working settings if the file is temporarily invalid.
                }
            }, dispatcher);

        _settingsWatcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        FileSystemEventHandler changed = (_, _) =>
            dispatcher.BeginInvoke(new Action(() => _settingsReloadTimer?.Start()));
        RenamedEventHandler renamed = (_, _) =>
            dispatcher.BeginInvoke(new Action(() => _settingsReloadTimer?.Start()));

        _settingsWatcher.Changed += changed;
        _settingsWatcher.Created += changed;
        _settingsWatcher.Renamed += renamed;
    }
}

internal sealed class SnowOverlay : Window
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private readonly Canvas _canvas = new();
    private readonly List<Flake> _flakes = [];
    private readonly System.Windows.Shapes.Path _snowbank = new()
    {
        Fill = new SolidColorBrush(Color.FromArgb(224, 242, 249, 255)),
        IsHitTestVisible = false
    };
    private readonly double[] _snowDepth;
    private const double MaxSnowDepth = 38;
    private const double SnowSegmentWidth = 12;
    private readonly Random _random = new();
    private SnowSettings _settings;
    private readonly System.Drawing.Rectangle _screenBounds;
    private readonly Dictionary<IntPtr, System.Windows.Shapes.Path> _windowSnow = [];
    private readonly DispatcherTimer _windowRefreshTimer =
        new() { Interval = TimeSpan.FromMilliseconds(700) };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private double _elapsedSeconds;
    private double _nextDensityChange;
    private int _targetParticleCount;
    private int _snowRenderFrame;

    public SnowOverlay(System.Drawing.Rectangle bounds, SnowSettings settings)
    {
        _settings = settings;
        _screenBounds = bounds;
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

        int segmentCount = Math.Max(2, (int)Math.Ceiling(Width / SnowSegmentWidth) + 1);
        _snowDepth = new double[segmentCount];
        _canvas.Children.Add(_snowbank);

        _targetParticleCount = settings.GetParticleCount();
        for (int i = 0; i < _targetParticleCount; i++)
            AddFlake(randomY: true);

        SourceInitialized += (_, _) => MakeClickThrough();
        Loaded += (_, _) =>
        {
            _timer.Start();
            _windowRefreshTimer.Start();
            RefreshWindowSnow();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _windowRefreshTimer.Stop();
        };
        _timer.Tick += (_, _) => Advance();
        _windowRefreshTimer.Tick += (_, _) => RefreshWindowSnow();
    }

    public void UpdateSettings(SnowSettings settings)
    {
        _settings = settings;
        _targetParticleCount = settings.GetParticleCount();
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

        // Variable intensity changes gradually during the session instead of choosing
        // a single random density only at launch.
        if (_settings.Intensity == SnowIntensity.Variable && _elapsedSeconds >= _nextDensityChange)
        {
            _targetParticleCount = _random.Next(45, 151);
            _nextDensityChange = _elapsedSeconds + _random.Next(4, 9);
        }

        while (_flakes.Count < _targetParticleCount)
            AddFlake(randomY: false);

        while (_flakes.Count > _targetParticleCount)
        {
            var last = _flakes[^1];
            _canvas.Children.Remove(last.Shape);
            _flakes.RemoveAt(_flakes.Count - 1);
        }

        double width = ActualWidth > 0 ? ActualWidth : Width;
        double height = ActualHeight > 0 ? ActualHeight : Height;
        double gust = Math.Sin(_elapsedSeconds * 0.42) * Math.Abs(_settings.Wind) * 0.65;
        double wind = _settings.Wind + gust;

        bool snowChanged = false;
        for (int i = 0; i < _snowDepth.Length; i++)
        {
            if (_snowDepth[i] > 0)
            {
                _snowDepth[i] = Math.Max(0, _snowDepth[i] - 0.0015);
                snowChanged = true;
            }
        }

        foreach (var flake in _flakes)
        {
            double x = Canvas.GetLeft(flake.Shape) + flake.Drift + wind * 0.035;
            double sway = Math.Sin(_elapsedSeconds * 1.7 + flake.Phase) * 0.18;
            double y = Canvas.GetTop(flake.Shape) + flake.Speed;
            x += sway;

            if (x < -8) x = width + 4;
            if (x > width + 8) x = -4;

            int segment = Math.Clamp((int)(x / SnowSegmentWidth), 0, _snowDepth.Length - 1);
            double flakeSize = flake.Shape.Width;
            if (y + flakeSize >= height - _snowDepth[segment])
            {
                DepositSnow(segment, flake.Speed);
                snowChanged = true;
                y = -10 - _random.NextDouble() * 20;
                x = _random.NextDouble() * width;
            }

            Canvas.SetLeft(flake.Shape, x);
            Canvas.SetTop(flake.Shape, y);
        }

        if (snowChanged && ++_snowRenderFrame >= 6)
        {
            _snowRenderFrame = 0;
            UpdateSnowbank(width, height);
        }
    }

    private void RefreshWindowSnow()
    {
        var visibleWindows = new HashSet<IntPtr>();
        var overlayHandle = new WindowInteropHelper(this).Handle;

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (hwnd == overlayHandle || !NativeMethods.IsWindowVisible(hwnd))
                return true;

            if ((NativeMethods.GetWindowLong(hwnd, -20) & 0x80) != 0)
                return true;

            var className = new System.Text.StringBuilder(256);
            NativeMethods.GetClassName(hwnd, className, className.Capacity);
            string classText = className.ToString();
            if (classText is "Shell_TrayWnd" or "Progman" or "WorkerW" or "Windows.UI.Core.CoreWindow")
                return true;

            if (!NativeMethods.GetWindowRect(hwnd, out var rect))
                return true;

            int windowWidth = rect.Right - rect.Left;
            int windowHeight = rect.Bottom - rect.Top;
            if (windowWidth < 100 || windowHeight < 60)
                return true;

            // Ignore windows outside this display and windows that are minimized.
            if (rect.Right <= _screenBounds.Left || rect.Left >= _screenBounds.Right ||
                rect.Bottom <= _screenBounds.Top || rect.Top >= _screenBounds.Bottom)
                return true;

            visibleWindows.Add(hwnd);
            if (!_windowSnow.TryGetValue(hwnd, out var cap))
            {
                cap = new System.Windows.Shapes.Path
                {
                    Fill = new SolidColorBrush(Color.FromArgb(235, 245, 250, 255)),
                    IsHitTestVisible = false
                };
                _windowSnow.Add(hwnd, cap);
                _canvas.Children.Add(cap);
                Canvas.SetZIndex(cap, 10);
            }

            double left = rect.Left - _screenBounds.Left;
            double top = rect.Top - _screenBounds.Top - 5;
            double right = rect.Right - _screenBounds.Left;
            double width = Math.Max(1, right - left);
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(0, 5), true, true);
                context.LineTo(new Point(0, 2), true, false);
                context.QuadraticBezierTo(new Point(width * 0.18, -1), new Point(width * 0.30, 2), true, false);
                context.QuadraticBezierTo(new Point(width * 0.48, -2), new Point(width * 0.62, 2), true, false);
                context.QuadraticBezierTo(new Point(width * 0.82, -1), new Point(width, 2), true, false);
                context.LineTo(new Point(width, 5), true, false);
            }
            geometry.Freeze();
            cap.Data = geometry;
            Canvas.SetLeft(cap, left);
            Canvas.SetTop(cap, top);
            return true;
        }, IntPtr.Zero);

        foreach (var stale in _windowSnow.Keys.Where(hwnd => !visibleWindows.Contains(hwnd)).ToArray())
        {
            _canvas.Children.Remove(_windowSnow[stale]);
            _windowSnow.Remove(stale);
        }
    }

    private void DepositSnow(int segment, double fallingSpeed)
    {
        double amount = 0.35 + Math.Min(0.8, fallingSpeed * 0.12);
        _snowDepth[segment] = Math.Min(MaxSnowDepth, _snowDepth[segment] + amount);

        if (segment > 0)
            _snowDepth[segment - 1] = Math.Min(MaxSnowDepth, _snowDepth[segment - 1] + amount * 0.22);
        if (segment < _snowDepth.Length - 1)
            _snowDepth[segment + 1] = Math.Min(MaxSnowDepth, _snowDepth[segment + 1] + amount * 0.22);
    }

    private void UpdateSnowbank(double width, double height)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(0, height), true, true);
            context.LineTo(new Point(0, height - _snowDepth[0]), true, false);

            for (int i = 1; i < _snowDepth.Length; i++)
            {
                double x = Math.Min(width, i * SnowSegmentWidth);
                context.LineTo(new Point(x, height - _snowDepth[i]), true, false);
            }

            context.LineTo(new Point(width, height), true, false);
        }

        geometry.Freeze();
        _snowbank.Data = geometry;
    }

    private sealed record Flake(Ellipse Shape, double Speed, double Drift, double Phase);

    private static class NativeMethods
    {
        internal delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder className, int maxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
}
