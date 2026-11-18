using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SkyrimAutoDiag
{
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x0000;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Eigenen Skyrim-Runen-Mauszeiger (ohne externe Dateien) auf das Hauptfenster anwenden
            CustomCursorHelper.ApplyToWindow(this);

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _viewModel.HotkeyChanged += RegisterConfiguredHotkey;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterConfiguredHotkey();
        }

        private void RegisterConfiguredHotkey()
        {
            if (_windowHandle == IntPtr.Zero) return;

            UnregisterHotKey(_windowHandle, HOTKEY_ID);

            string keyName = _viewModel.ScreenshotHotkey;
            if (Enum.TryParse<Key>(keyName, true, out Key parsedKey))
            {
                uint vk = (uint)KeyInterop.VirtualKeyFromKey(parsedKey);
                if (vk != 0)
                {
                    RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, vk);
                }
            }
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            Key pressedKey = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (pressedKey == Key.LeftShift || pressedKey == Key.RightShift ||
                pressedKey == Key.LeftCtrl || pressedKey == Key.RightCtrl ||
                pressedKey == Key.LeftAlt || pressedKey == Key.RightAlt ||
                pressedKey == Key.Escape || pressedKey == Key.Tab)
            {
                return;
            }

            _viewModel.ScreenshotHotkey = pressedKey.ToString();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                _viewModel.CaptureInGameMicScreenshot();
                handled = true;
            }
            return IntPtr.Zero;
        }

        // Startet das Skyrim Invaders Minispiel in einem neuen Fenster (mit eigenem Mauszeiger)
        private void OpenMiniGame_Click(object sender, RoutedEventArgs e)
        {
            var gameWindow = new SkyrimInvadersWindow
            {
                Owner = this
            };
            CustomCursorHelper.ApplyToWindow(gameWindow);
            gameWindow.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source?.RemoveHook(HwndHook);
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
            }
            base.OnClosed(e);
        }
    }
}