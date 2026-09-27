using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace PrivateAI.Windows;

public partial class MainWindow : Window
{
    // =========================================================
    // AI PROVIDERS
    // =========================================================

    private static readonly Uri ChatGPT =
        new("https://chatgpt.com/");

    private static readonly Uri Claude =
        new("https://claude.ai/");

    private static readonly Uri Gemini =
        new("https://gemini.google.com/");

    private static readonly Uri Perplexity =
        new("https://www.perplexity.ai/");


    // =========================================================
    // WEBVIEW2
    // =========================================================

    private CoreWebView2Environment? _webViewEnvironment;


    // =========================================================
    // NATIVE WINDOW HOOK
    // =========================================================

    private HwndSource? _hwndSource;


    // =========================================================
    // GLOBAL HOTKEYS
    // =========================================================

    private const int WM_HOTKEY = 0x0312;

    private const int HideShowHotkeyId = 5001;

    private const int ScreenshotHotkeyId = 5002;

    private const uint MOD_CONTROL = 0x0002;

    private const uint MOD_SHIFT = 0x0004;

    private const uint MOD_NOREPEAT = 0x4000;

    private const uint VK_SPACE = 0x20;

    private const uint VK_S = 0x53;


    // =========================================================
    // SCREEN CAPTURE PROTECTION
    // =========================================================

    private const uint WDA_NONE = 0x00000000;

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;


    // =========================================================
    // TOOL WINDOW STYLE
    // =========================================================

    private const int GWL_EXSTYLE = -20;

    private const long WS_EX_APPWINDOW = 0x00040000L;

    private const long WS_EX_TOOLWINDOW = 0x00000080L;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;

        Closed += MainWindow_Closed;
    }


    // =========================================================
    // WINDOW LOADED
    // =========================================================

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            // Make Private AI a floating tool window.
            ConfigureToolWindow();

            // Register global keyboard shortcuts.
            RegisterGlobalHotkeys();

            // Hide Private AI from supported screen captures.
            ApplyCaptureProtection();


            // =================================================
            // WEBVIEW2 USER DATA
            // =================================================

            string userDataFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "PrivateAI",
                    "WebView2");

            Directory.CreateDirectory(
                userDataFolder);


            // =================================================
            // WEBVIEW2 ENVIRONMENT
            // =================================================

            _webViewEnvironment =
                await CoreWebView2Environment.CreateAsync(
                    null,
                    userDataFolder);


            // =================================================
            // INITIALIZE WEBVIEW2
            // =================================================

            await Browser.EnsureCoreWebView2Async(
                _webViewEnvironment);


            // =================================================
            // WEBVIEW2 ZOOM
            // =================================================

            Browser.ZoomFactor = 0.85;


            // =================================================
            // NAVIGATION EVENT
            // =================================================

            Browser.CoreWebView2.NavigationCompleted +=
                CoreWebView2_NavigationCompleted;


            // =================================================
            // DEFAULT PAGE
            // =================================================

            Browser.CoreWebView2.Navigate(
                ChatGPT.ToString());
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "Private AI could not start correctly.\n\n" +
                ex.Message,
                "Private AI",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    // =========================================================
    // CONFIGURE TOOL WINDOW
    // =========================================================

    private void ConfigureToolWindow()
    {
        var helper =
            new WindowInteropHelper(this);

        if (helper.Handle == IntPtr.Zero)
            return;


        IntPtr currentStyle =
            GetWindowLongPtr(
                helper.Handle,
                GWL_EXSTYLE);


        long exStyle =
            currentStyle.ToInt64();


        // Remove normal application window style.
        exStyle &= ~WS_EX_APPWINDOW;


        // Make it a tool window.
        exStyle |= WS_EX_TOOLWINDOW;


        SetWindowLongPtr(
            helper.Handle,
            GWL_EXSTYLE,
            new IntPtr(exStyle));
    }


    // =========================================================
    // GLOBAL HOTKEYS
    // =========================================================

    private void RegisterGlobalHotkeys()
    {
        var helper =
            new WindowInteropHelper(this);

        IntPtr handle =
            helper.Handle;

        if (handle == IntPtr.Zero)
            return;


        _hwndSource =
            HwndSource.FromHwnd(handle);


        if (_hwndSource != null)
        {
            _hwndSource.AddHook(WndProc);
        }


        // =====================================================
        // CTRL + SHIFT + SPACE
        // =====================================================

        bool hideShowRegistered =
            RegisterHotKey(
                handle,
                HideShowHotkeyId,
                MOD_CONTROL |
                MOD_SHIFT |
                MOD_NOREPEAT,
                VK_SPACE);


        if (!hideShowRegistered)
        {
            System.Windows.MessageBox.Show(
                "Ctrl + Shift + Space could not be registered.\n\n" +
                "Another application may already be using this shortcut.",
                "Private AI",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }


        // =====================================================
        // CTRL + SHIFT + S
        // =====================================================

        bool screenshotRegistered =
            RegisterHotKey(
                handle,
                ScreenshotHotkeyId,
                MOD_CONTROL |
                MOD_SHIFT |
                MOD_NOREPEAT,
                VK_S);


        if (!screenshotRegistered)
        {
            System.Windows.MessageBox.Show(
                "Ctrl + Shift + S could not be registered.\n\n" +
                "Another application may already be using this shortcut.",
                "Private AI",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }


    // =========================================================
    // WINDOWS MESSAGE HOOK
    // =========================================================

    private IntPtr WndProc(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int hotkeyId =
                wParam.ToInt32();


            // Ctrl + Shift + Space
            if (hotkeyId ==
                HideShowHotkeyId)
            {
                ToggleWindowVisibility();

                handled = true;
            }


            // Ctrl + Shift + S
            else if (hotkeyId ==
                     ScreenshotHotkeyId)
            {
                CaptureScreenshotSilently();

                handled = true;
            }
        }

        return IntPtr.Zero;
    }


    // =========================================================
    // HIDE / SHOW
    // =========================================================

    private void ToggleWindowVisibility()
    {
        if (IsVisible)
        {
            Hide();

            return;
        }


        Show();

        WindowState =
            WindowState.Normal;

        Activate();


        // Bring window to front.
        Topmost = true;

        Topmost = false;
    }


    // =========================================================
    // SCREENSHOT
    // =========================================================

    private static void CaptureScreenshotSilently()
    {
        try
        {
            ScreenshotService
                .CapturePrimaryScreen();
        }
        catch
        {
            // Intentionally silent.
        }
    }


    // =========================================================
    // SCREEN CAPTURE EXCLUSION
    // =========================================================

    private void ApplyCaptureProtection()
    {
        var helper =
            new WindowInteropHelper(this);


        if (helper.Handle == IntPtr.Zero)
            return;


        bool result =
            SetWindowDisplayAffinity(
                helper.Handle,
                WDA_EXCLUDEFROMCAPTURE);


        if (!result)
        {
            int error =
                Marshal.GetLastWin32Error();


            System.Diagnostics.Debug.WriteLine(
                "SetWindowDisplayAffinity failed. " +
                $"Error: {error}");
        }
    }


    // =========================================================
    // WEBVIEW NAVIGATION
    // =========================================================

    private void CoreWebView2_NavigationCompleted(
        object? sender,
        CoreWebView2NavigationCompletedEventArgs e)
    {
        // WebView2 native zoom is already 0.85.
    }


    // =========================================================
    // PROVIDER BUTTONS
    // =========================================================

    private void ChatGPT_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate(ChatGPT);
    }


    private void Claude_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate(Claude);
    }


    private void Gemini_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate(Gemini);
    }


    private void Perplexity_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate(Perplexity);
    }


    // =========================================================
    // NAVIGATION
    // =========================================================

    private void Navigate(Uri url)
    {
        if (Browser.CoreWebView2 != null)
        {
            Browser.CoreWebView2.Navigate(
                url.ToString());
        }
    }


    // =========================================================
    // NORMAL WINDOW DRAG
    // =========================================================

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.LeftButton ==
            MouseButtonState.Pressed)
        {
            DragMove();
        }
    }


    // =========================================================
    // CLOSE
    // =========================================================

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void MainWindow_Closed(
        object? sender,
        EventArgs e)
    {
        var helper =
            new WindowInteropHelper(this);


        if (helper.Handle != IntPtr.Zero)
        {
            UnregisterHotKey(
                helper.Handle,
                HideShowHotkeyId);

            UnregisterHotKey(
                helper.Handle,
                ScreenshotHotkeyId);
        }


        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(
                WndProc);

            _hwndSource.Dispose();

            _hwndSource = null;
        }
    }


    // =========================================================
    // WIN32 - HOTKEY
    // =========================================================

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);


    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);


    // =========================================================
    // WIN32 - SCREEN CAPTURE
    // =========================================================

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(
        IntPtr hWnd,
        uint dwAffinity);


    // =========================================================
    // WIN32 - WINDOW STYLE
    // =========================================================

    [DllImport(
        "user32.dll",
        EntryPoint = "GetWindowLongPtr",
        SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(
        IntPtr hWnd,
        int nIndex);


    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLongPtr",
        SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        IntPtr dwNewLong);


    // =========================================================
    // SCREENSHOT SERVICE
    // =========================================================

    private static class ScreenshotService
    {
        public static string GetFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures),
                "Screenshots");
        }


        public static void CapturePrimaryScreen()
        {
            Screen? screen =
                Screen.PrimaryScreen;


            if (screen == null)
                return;


            Rectangle bounds =
                screen.Bounds;


            // Create screenshot bitmap.
            using var bitmap =
                new Bitmap(
                    bounds.Width,
                    bounds.Height,
                    PixelFormat.Format32bppArgb);


            // Capture primary screen.
            using (Graphics graphics =
                   Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    bounds.Location,
                    System.Drawing.Point.Empty,
                    bounds.Size,
                    CopyPixelOperation.SourceCopy);
            }


            // Screenshot folder.
            string folder =
                GetFolder();


            Directory.CreateDirectory(
                folder);


            // Screenshot filename.
            string filename =
                "Screenshot_" +
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss_fff") +
                ".png";


            string path =
                Path.Combine(
                    folder,
                    filename);


            // Save PNG.
            bitmap.Save(
                path,
                ImageFormat.Png);


            // Copy latest screenshot to clipboard.
            using var clipboardBitmap =
                new Bitmap(bitmap);


            System.Windows.Forms.Clipboard.SetImage(
                clipboardBitmap);
        }
    }
}