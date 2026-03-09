using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;

namespace NeonProject
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_LCONTROL = 0xA2;
        private const byte VK_E = 0x45;
        private const byte SCAN_E = 0x12; 
        private const int HOTKEY_ID = 9000;

        private bool _isRunning = false;
        private bool _isCapturingKey = false;
        private string _stopKey = "Escape";
        private uint _stopKeyVk = 0x1B;
        private string configPath = "settings.cfg";

        public MainWindow() { InitializeComponent(); LoadSettings(); }

        // Human-like smooth mouse movement
        private async Task MoveMouseSmoothly(int targetX, int targetY, int steps = 15) {
            GetCursorPos(out POINT currentPos);
            for (int i = 1; i <= steps; i++) {
                if (!_isRunning) break;
                int intermediateX = currentPos.X + (targetX - currentPos.X) * i / steps;
                int intermediateY = currentPos.Y + (targetY - currentPos.Y) * i / steps;
                SetCursorPos(intermediateX, intermediateY);
                await Task.Delay(10); // Small delay between steps
            }
            SetCursorPos(targetX, targetY);
            await Task.Delay(80); // Hover for a split second
        }

        protected override void OnSourceInitialized(EventArgs e) { base.OnSourceInitialized(e); UpdateHotKey(); }

        private void UpdateHotKey() {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            RegisterHotKey(helper.Handle, HOTKEY_ID, 0, _stopKeyVk);
            ComponentDispatcher.ThreadPreprocessMessage += (ref MSG msg, ref bool handled) => {
                if (msg.message == 0x0312 && (int)msg.wParam == HOTKEY_ID) {
                    _isRunning = false;
                    keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, 0); 
                    this.Dispatcher.Invoke(() => { if(StatusLabel != null) StatusLabel.Text = "STOPPED BY HOTKEY"; });
                }
            };
        }

        private void LoadSettings() {
            if (File.Exists(configPath)) {
                try {
                    string[] lines = File.ReadAllLines(configPath);
                    if (lines.Length >= 3) {
                        _stopKey = lines[0] ?? "Escape"; 
                        StopKeyBtn.Content = _stopKey;
                        if (Enum.TryParse(typeof(Key), _stopKey, out object? k) && k != null) 
                            _stopKeyVk = (uint)KeyInterop.VirtualKeyFromKey((Key)k);
                        RunCountInput.Text = lines[1]; 
                        InfiniteCheck.IsChecked = bool.Parse(lines[2]);
                    }
                } catch { }
            }
        }

        private void SaveSettings() { try { File.WriteAllLines(configPath, new string[] { _stopKey, RunCountInput.Text, (InfiniteCheck.IsChecked ?? false).ToString() }); } catch { } }
        private void OpenSettings(object sender, RoutedEventArgs e) { WelcomePanel.Visibility = Visibility.Collapsed; SettingsPanel.Visibility = Visibility.Visible; }
        private void CloseSettings(object sender, RoutedEventArgs e) { SaveSettings(); UpdateHotKey(); SettingsPanel.Visibility = Visibility.Collapsed; WelcomePanel.Visibility = Visibility.Visible; }
        private void StartKeyCapture(object sender, RoutedEventArgs e) { _isCapturingKey = true; StopKeyBtn.Content = "..."; }
        private void Window_KeyDown(object sender, KeyEventArgs e) { if (_isCapturingKey) { _stopKey = e.Key.ToString(); _stopKeyVk = (uint)KeyInterop.VirtualKeyFromKey(e.Key); StopKeyBtn.Content = _stopKey; _isCapturingKey = false; } }

        private async void RunAutomation(object sender, RoutedEventArgs e) {
            if (_isRunning || !(sender is Button btn)) return;
            string mode = btn.Content?.ToString()?.ToUpper() ?? "";
            _isRunning = true;
            StatusLabel.Text = "STARTING IN 2s...";
            await Task.Delay(2000);

            Random rnd = new Random();
            int.TryParse(RunCountInput.Text, out int runs);
            int count = 0;

            while (_isRunning && (InfiniteCheck.IsChecked == true || count < runs)) {
                if (mode.Contains("RECYCLE")) {
                    int[,] targetCoords;
                    int actionX, actionY, confirmX, confirmY;

                    if (mode.Contains("GUNS")) {
                        targetCoords = new int[,] { {200,300}, {300,300}, {400,300}, {500,300}, {200,400}, {300,400}, {400,400}, {500,400}, {200,500}, {300,500}, {400,500}, {500,500} };
                        actionX = 650; actionY = 660; confirmX = 1000; confirmY = 750;
                    } else {
                        targetCoords = new int[,] { {200,300}, {300,300}, {400,300}, {500,300}, {200,400}, {300,400}, {400,400}, {500,400}, {200,500}, {300,500}, {400,500}, {500,500} };
                        actionX = 650; actionY = 660; confirmX = 1000; confirmY = 750;
                    }

                    keybd_event(VK_LCONTROL, 0, 0, 0); 
                    await Task.Delay(100);

                    for (int i = 0; i < 12 && _isRunning; i++) {
                        await MoveMouseSmoothly(targetCoords[i,0], targetCoords[i,1], 8);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0); mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        await Task.Delay(rnd.Next(150, 250));
                    }

                    if (_isRunning) {
                        keybd_event(VK_LCONTROL, 0, KEYEVENTF_KEYUP, 0); 
                        await Task.Delay(200);

                        // Move to 12th item and Right Click
                        await MoveMouseSmoothly(targetCoords[11,0], targetCoords[11,1], 10);
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0); mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                        
                        await Task.Delay(rnd.Next(1000, 1300)); // Wait for menu to fully open

                        // Human move to "Action" button
                        await MoveMouseSmoothly(actionX, actionY, 12);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0); mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        
                        await Task.Delay(rnd.Next(600, 800));

                        // Human move to "Confirm" button
                        await MoveMouseSmoothly(confirmX, confirmY, 12);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0); mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        
                        await Task.Delay(rnd.Next(1500, 2000));
                    }
                }
                else {
                    // Upgrader/Crafter logic
                    keybd_event(VK_E, SCAN_E, 0, 0); await Task.Delay(800); keybd_event(VK_E, SCAN_E, KEYEVENTF_KEYUP, 0); await Task.Delay(350);
                }
                count++;
            }
            _isRunning = false;
            if (StatusLabel != null && StatusLabel.Text != "STOPPED BY HOTKEY") StatusLabel.Text = "FINISHED";
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void CloseApp(object sender, RoutedEventArgs e) { var helper = new WindowInteropHelper(this); UnregisterHotKey(helper.Handle, HOTKEY_ID); SaveSettings(); Application.Current.Shutdown(); }
    }
}