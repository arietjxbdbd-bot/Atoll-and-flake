using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Flake.Integration;

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
                foreach (var screen in Forms.Screen.AllScreens)
                {
                    var overlay = new SnowOverlay(screen.Bounds);
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
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private const int FlakeCount = 95;

    public SnowOverlay(System.Drawing.Rectangle bounds)
    {
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

        for (int i = 0; i < FlakeCount; i++)
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
        double size = 1.5 + _random.NextDouble() * 4.5;
        var shape = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromArgb((byte)(110 + _random.Next(100)), 255, 255, 255)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(shape, _random.NextDouble() * Math.Max(1, ActualWidth > 0 ? ActualWidth : Width));
        Canvas.SetTop(shape, randomY ? _random.NextDouble() * Math.Max(1, ActualHeight > 0 ? ActualHeight : Height) : -10);
        _canvas.Children.Add(shape);
        _flakes.Add(new Flake(shape, 0.45 + _random.NextDouble() * 1.55, (_random.NextDouble() - 0.5) * 0.55));
    }

    private void Advance()
    {
        double width = ActualWidth > 0 ? ActualWidth : Width;
        double height = ActualHeight > 0 ? ActualHeight : Height;

        foreach (var flake in _flakes)
        {
            double x = Canvas.GetLeft(flake.Shape) + flake.Drift;
            double y = Canvas.GetTop(flake.Shape) + flake.Speed;
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

    private sealed record Flake(Ellipse Shape, double Speed, double Drift);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
}
