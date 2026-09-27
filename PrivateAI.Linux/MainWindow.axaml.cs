using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SharpHook;
using SharpHook.Data;

namespace PrivateAI.Linux;

public partial class MainWindow : Window
{
    private static readonly Uri ChatGPT =
        new("https://chatgpt.com/");

    private static readonly Uri Claude =
        new("https://claude.ai/");

    private static readonly Uri Gemini =
        new("https://gemini.google.com/");

    private static readonly Uri Perplexity =
        new("https://www.perplexity.ai/");

    private readonly SimpleGlobalHook _globalHook = new();
    private readonly CancellationTokenSource _hotkeyCancellation = new();

    private bool _ctrlPressed;
    private bool _shiftPressed;
    private bool _spacePressed;
    private bool _sPressed;

    public MainWindow()
    {
        InitializeComponent();

        Browser.NavigationStarted += Browser_NavigationStarted;
        Browser.NavigationCompleted += Browser_NavigationCompleted;

        OpenProvider(ChatGPT);

        StartGlobalHotkey();
    }

    // ============================================================
    // GLOBAL HOTKEY
    // ============================================================

    private void StartGlobalHotkey()
    {
        _globalHook.KeyPressed += GlobalHook_KeyPressed;
        _globalHook.KeyReleased += GlobalHook_KeyReleased;

        _ = Task.Run(async () =>
        {
            try
            {
                await _globalHook.RunAsync();
            }
            catch
            {
                // Linux desktop/session may restrict global hooks.
            }
        });
    }

    private void GlobalHook_KeyPressed(
        object? sender,
        KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _ctrlPressed = true;
                break;

            case KeyCode.VcLeftShift:
            case KeyCode.VcRightShift:
                _shiftPressed = true;
                break;

            case KeyCode.VcSpace:
                _spacePressed = true;
                break;

            case KeyCode.VcS:
                _sPressed = true;
                break;
        }

        // Ctrl + Shift + Space
        if (_ctrlPressed &&
            _shiftPressed &&
            _spacePressed)
        {
            _spacePressed = false;

            Avalonia.Threading.Dispatcher.UIThread.Post(
                ToggleWindowVisibility);
        }

        // Ctrl + Shift + S
        if (_ctrlPressed &&
            _shiftPressed &&
            _sPressed)
        {
            _sPressed = false;

            Avalonia.Threading.Dispatcher.UIThread.Post(
                CaptureScreenshotSilently);
        }
    }

    private void GlobalHook_KeyReleased(
        object? sender,
        KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _ctrlPressed = false;
                break;

            case KeyCode.VcLeftShift:
            case KeyCode.VcRightShift:
                _shiftPressed = false;
                break;

            case KeyCode.VcSpace:
                _spacePressed = false;
                break;

            case KeyCode.VcS:
                _sPressed = false;
                break;
        }
    }

    // ============================================================
    // HIDE / SHOW
    // ============================================================

    private void ToggleWindowVisibility()
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    // ============================================================
    // SCREENSHOT
    // ============================================================

    private void CaptureScreenshotSilently()
    {
        try
        {
            string screenshotFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures),
                "Screenshots");

            Directory.CreateDirectory(screenshotFolder);

            string filename =
                "Screenshot_" +
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss_fff") +
                ".png";

            string path =
                Path.Combine(
                    screenshotFolder,
                    filename);

            string sessionType =
                Environment.GetEnvironmentVariable(
                    "XDG_SESSION_TYPE")
                ?.ToLowerInvariant() ?? "";

            bool success;

            if (sessionType == "wayland")
            {
                success = CaptureWayland(path);
            }
            else
            {
                success = CaptureX11(path);
            }

            if (!success || !File.Exists(path))
                return;

            CopyImageToClipboard(path);
        }
        catch
        {
            // Intentionally silent.
        }
    }

    private static bool CaptureWayland(string path)
    {
        // grim is the lightweight Wayland screenshot utility.
        return RunCommand(
            "grim",
            Quote(path));
    }

    private static bool CaptureX11(string path)
    {
        // Try gnome-screenshot first.
        if (CommandExists("gnome-screenshot"))
        {
            return RunCommand(
                "gnome-screenshot",
                "-f " + Quote(path));
        }

        // Fall back to scrot.
        if (CommandExists("scrot"))
        {
            return RunCommand(
                "scrot",
                Quote(path));
        }

        return false;
    }

    private static void CopyImageToClipboard(string path)
    {
        string sessionType =
            Environment.GetEnvironmentVariable(
                "XDG_SESSION_TYPE")
            ?.ToLowerInvariant() ?? "";

        if (sessionType == "wayland")
        {
            // wl-copy reads the PNG from stdin.
            CopyUsingWlCopy(path);
            return;
        }

        // X11 clipboard.
        if (CommandExists("xclip"))
        {
            CopyUsingXclip(path);
            return;
        }

        if (CommandExists("xsel"))
        {
            CopyUsingXsel(path);
        }
    }

    private static void CopyUsingWlCopy(string path)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wl-copy",
                Arguments = "--type image/png",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        using (FileStream file =
               File.OpenRead(path))
        {
            file.CopyTo(
                process.StandardInput.BaseStream);
        }

        process.StandardInput.Close();
        process.WaitForExit();
    }

    private static void CopyUsingXclip(string path)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xclip",
                Arguments =
                    "-selection clipboard " +
                    "-t image/png " +
                    "-i",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        using (FileStream file =
               File.OpenRead(path))
        {
            file.CopyTo(
                process.StandardInput.BaseStream);
        }

        process.StandardInput.Close();
        process.WaitForExit();
    }

    private static void CopyUsingXsel(string path)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xsel",
                Arguments =
                    "--clipboard " +
                    "--input",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        using (FileStream file =
               File.OpenRead(path))
        {
            file.CopyTo(
                process.StandardInput.BaseStream);
        }

        process.StandardInput.Close();
        process.WaitForExit();
    }

    private static bool CommandExists(string command)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool RunCommand(
        string command,
        string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string Quote(string value)
    {
        return "\"" +
               value.Replace("\"", "\\\"") +
               "\"";
    }

    // ============================================================
    // BROWSER
    // ============================================================

    private void Browser_NavigationStarted(
        object? sender,
        EventArgs e)
    {
    }

    private void Browser_NavigationCompleted(
        object? sender,
        EventArgs e)
    {
        ApplyBrowserZoom();
    }

    private void ApplyBrowserZoom()
    {
        try
        {
            string script =
                "document.documentElement.style.zoom = '0.90';" +
                "document.body.style.zoom = '0.90';";

            Browser.InvokeScript(script);
        }
        catch
        {
        }
    }

    private void OpenProvider(Uri url)
    {
        Browser.Navigate(url);
    }

    // ============================================================
    // PROVIDERS
    // ============================================================

    private void ChatGPT_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenProvider(ChatGPT);
    }

    private void Claude_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenProvider(Claude);
    }

    private void Gemini_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenProvider(Gemini);
    }

    private void Perplexity_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenProvider(Perplexity);
    }

    // ============================================================
    // WINDOW
    // ============================================================

    private void TitleBar_PointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this)
            .Properties
            .IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    protected override void OnClosed(EventArgs e)
    {
        _hotkeyCancellation.Cancel();

        _globalHook.KeyPressed -=
            GlobalHook_KeyPressed;

        _globalHook.KeyReleased -=
            GlobalHook_KeyReleased;

        _globalHook.Dispose();
        _hotkeyCancellation.Dispose();

        base.OnClosed(e);
    }
}