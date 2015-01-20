using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoScreenshot {
    static class Program {

        private const string SAVE_FORMAT = "{0:yyyy-MM-dd HH-mm-ss ffff}.png";

        private static IntPtr keyboardHookKey;

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MenuItem exitMenuItem = new MenuItem() { Text = "E&xit" };
            exitMenuItem.Click += new EventHandler(exitMenuItem_Click);

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.AddRange(new MenuItem[] { exitMenuItem });

            var bmp = Properties.Resources.app;
            var notifyIcon = new NotifyIcon() {
                Text = "Auto Screenshot",
                ContextMenu = contextMenu,
                Icon = Icon.FromHandle(bmp.GetHicon()),
                Visible = true
            };

            keyboardHookKey = SetHook(HookCallback);

            Application.Run();

            UnhookWindowsHookEx(keyboardHookKey);
            notifyIcon.Visible = false;
        }

        private static IntPtr SetHook(KeyboardProcedure proc) {
            using (var currentProcess = Process.GetCurrentProcess()) {
                using (var mainModule = currentProcess.MainModule) {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(mainModule.ModuleName), 0);
                }
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)) {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = ((Keys)vkCode);

                if (Keys.Alt == Control.ModifierKeys && Keys.PrintScreen == key) {
                    CaptureActiveWindow();
                } else if (key == Keys.PrintScreen) {
                    CaptureEverything();
                }
            }

            return CallNextHookEx(keyboardHookKey, nCode, wParam, lParam);
        }


        #region Capture the screen code

        private static void CaptureEverything() {
            CaptureScreenAndSave(0, 0, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        }

        private static void CaptureActiveWindow() {
            var rect = new Rect();
            GetWindowRect(GetForegroundWindow(), ref rect);
            CaptureScreenAndSave(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        private static void CaptureScreenAndSave(int left, int top, int width, int height) {
            using (var bmp = new Bitmap(width, height)) {
                using (var g = Graphics.FromImage(bmp)) {
                    g.CopyFromScreen(left, top, 0, 0, bmp.Size);
                }

                var saveLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), string.Format(SAVE_FORMAT, DateTime.Now));
                bmp.Save(saveLocation, ImageFormat.Png);
            }
        }

        #endregion

        private static void exitMenuItem_Click(object Sender, EventArgs e) {
            Application.Exit();
        }

        #region External

        private delegate IntPtr KeyboardProcedure(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProcedure lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        #endregion
    }
}
