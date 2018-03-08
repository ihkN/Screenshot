using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenshotTool
{
    public partial class SystemTray : Form
    {
        private const int j = 0x4A;
        private const int w = 0x57;
        private ContextMenu _ctxMenu;
        private bool _ctxAutostart;
        private const string _appName = "ScreenshotTool";
        private static readonly string AppPath = Path.GetFullPath(@"./ScreenshotTool.exe");
        private readonly KeyboardHook _hook = new KeyboardHook();

        private static NotifyIcon _notifier;
        public static NotifyIcon Notifier => _notifier;

        public SystemTray()
        {
            InitializeComponent();
            _hook.KeyPressed += hook_KeyPressed;
            _hook.RegisterHotKey(ScreenshotTool.ModifierKeys.Alt, Keys.S);
            _notifier = new NotifyIcon
            {
                Icon = Icon,
                Visible = true
            };
        }

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            var selectArea = new SelectArea();
            selectArea.Show();
            selectArea.Focus();
        }

        private void SystemTray_Load(object sender, EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            Notifier.ShowBalloonTip(1500, "We're ready to take some screenshots!", "Press ALT+S to get started.", ToolTipIcon.Info);

            UpdateContextMenu();
        }

        private void UpdateContextMenu()
        {
            var defaultPath = Path.GetFullPath(Properties.Settings.Default.DefaultPath);
            if (!Directory.Exists(defaultPath)) Directory.CreateDirectory(defaultPath);
            var files = Directory.EnumerateFiles(defaultPath, "*.png").Reverse().Take(10).ToList();

            _ctxMenu = new ContextMenu();

            var listItems = new List<MenuItem>
            {
                new MenuItem("Einstellungen", ctx_Settings),
                new MenuItem("mit Windows starten", ctx_Autostart),
                new MenuItem($"letzten Bilder ({files.Count})"),
                new MenuItem("Beenden", (o, args) => Application.Exit())
            };

            var recentScreenshots = new List<MenuItem>();
            var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var regValue = regKey?.GetValue(_appName);

            _ctxAutostart = regValue?.ToString() == AppPath;

            files.ForEach(file =>
            {
                var menuItem = new MenuItem(Path.GetFileName(file.ToString()), (o, args) =>
                {
                    using (var fs = new FileStream(file.ToString(), FileMode.Open))
                    {
                        var pic = new Bitmap(fs);
                        var preview = new PreviewArea((Bitmap)pic.Clone(), file.ToString());
                        preview.Show();
                    }
                });
                recentScreenshots.Add(menuItem);
            });

            listItems[2].MenuItems.AddRange(recentScreenshots.ToArray());

            listItems[1].Checked = _ctxAutostart;

            //todo: Entfernen sobald fertig
            listItems[0].Enabled = false;

            _ctxMenu.MenuItems.AddRange(listItems.ToArray());

            Notifier.ContextMenu = _ctxMenu;
            _ctxMenu.Popup += ctx_OnPopup;
        }

        private void ctx_OnPopup(object sender, EventArgs e)
        {
            UpdateContextMenu();
        }

        private void ctx_Settings(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ctx_Autostart(object sender, EventArgs e)
        {
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Debug.WriteLine($"ctxAutostart: {_ctxAutostart}, reg {key?.GetValue(_appName)}");
            if (!_ctxAutostart)
            {
                key?.SetValue(_appName, AppPath);
                _ctxMenu.MenuItems[1].Checked = true;
                _ctxAutostart = true;
            }
            else
            {
                key?.DeleteValue(_appName, false);
                _ctxMenu.MenuItems[1].Checked = false;
                _ctxAutostart = false;
            }
        }
    }
}
