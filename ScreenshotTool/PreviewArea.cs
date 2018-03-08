using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public partial class PreviewArea : Form
    {
        private const int j = 0x4A;
        private const int w = 0x57;
        public static Bitmap Screenshot;
        private ContextMenu _ctxMenu;
        private Point _startPoint;

        private bool _mouseIsDown;

        private bool _alwaysOnTop;
        private bool _locked;
        private double _transparency = 1;
        private bool _saved;

        private string _fileName;
        private string _filePath;

        public PreviewArea(Bitmap bmp, string path)
        {
            InitializeComponent();

            Screenshot = bmp;
            _alwaysOnTop = true;

            if (path == string.Empty)
            {
                _fileName = $"screenshot-{DateTime.Now:dd-mm-yyyy}";
                _filePath = Path.GetFullPath(Properties.Settings.Default.DefaultPath);
            }
            else
            {
                _saved = true;
                _filePath = Path.GetFullPath(path);
                _fileName = Path.GetFileName(_filePath);
            }
        }

        private void PreviewArea_Load(object sender, EventArgs e)
        {
            UpdateContextMenu();

            picturePreview.Image = Screenshot;
            picturePreview.BorderStyle = BorderStyle.Fixed3D;

            Text = _fileName;
        }

        private void UpdateContextMenu()
        {
            picturePreview.ContextMenu?.Dispose();
            _ctxMenu = new ContextMenu();

            var listItems = new List<MenuItem>();
            var visualItems = new List<MenuItem>
                {
                    new MenuItem("Immer im Vordergrund", ctx_alwaysOnTopClick),
                    new MenuItem("Sperren", ctx_locked),
                    new MenuItem($"Transparenz ({_transparency*100}%)", ctx_transparency),
                    new MenuItem("-")
            };
            var fileItems = new List<MenuItem>
                {
                    new MenuItem("Löschen", ctx_fileDelete),
                    new MenuItem("Bearbeiten", ctx_editImage),
                    new MenuItem("-")

            };
            var actionItems = new List<MenuItem>
                {
                    new MenuItem("Kopieren", ctx_copyToClipboard),
                    new MenuItem("Upload", ctx_Upload),
                    new MenuItem("Speichern", ctx_quickSave),
                    new MenuItem("Speichern unter", ctx_saveToDisk),
                    new MenuItem("-"),
                    new MenuItem("Schließen", ctx_exit)
                };

            listItems.AddRange(visualItems);
            if (_saved) listItems.AddRange(fileItems);
            listItems.AddRange(actionItems);

            listItems[0].Checked = _alwaysOnTop;

            _ctxMenu.MenuItems.AddRange(listItems.ToArray());
            picturePreview.ContextMenu = _ctxMenu;
        }

        private void ctx_Upload(object sender, EventArgs e)
        {
            using (var memoryStream = new MemoryStream())
            {
                Screenshot.Save(memoryStream, ImageFormat.Png);
                Upload(memoryStream.ToArray()).ContinueWith(t => Console.WriteLine(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private void ctx_fileDelete(object sender, EventArgs e)
        {
            File.Delete(_filePath);
            _saved = false;
            UpdateContextMenu();

        }

        private void ctx_editImage(object sender, EventArgs e)
        {
            //var x = new EditImage((Bitmap) _screenshot.Clone());
            //picturePreview.Image.Dispose();
            //Close();

            var x = new Editor.Form1(Screenshot);
            x.Show();

            picturePreview.Image.Dispose();
        }

        private static async Task Upload(byte[] image)
        {
            using (var client = new HttpClient())
            {
                using (var content =
                    new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    content.Add(new StreamContent(new MemoryStream(image)), "bilddatei", "upload.jpg");

                    using (
                        var message =
                            await client.PostAsync("http://www.directupload.net/index.php?mode=upload", content))
                    {
                        var input = await message.Content.ReadAsStringAsync();

                        Debug.WriteLine(!string.IsNullOrWhiteSpace(input)
                            ? Regex.Match(input, @"http://\w*\.directupload\.net/images/\d*/\w*\.[a-z]{3}").Value
                            : null);

                        var url = !string.IsNullOrWhiteSpace(input) ? Regex.Match(input, @"http://\w*\.directupload\.net/images/\d*/\w*\.[a-z]{3}").Value : null;
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            Clipboard.Clear();
                            Clipboard.SetText(url);
                            SystemTray.Notifier.ShowBalloonTip(2000, "Upload erfolgreich!", $"Das Bild ist unter  {url} erreichbar. STRG+V", ToolTipIcon.Info);
                        }
                    }
                }
            }
        }

        private void ctx_transparency(object sender, EventArgs e)
        {
            if (_transparency <= .25) _transparency = 1.25;
            _transparency = _transparency - .25;
            Opacity = _transparency;

            UpdateContextMenu();
        }

        private void ctx_locked(object sender, EventArgs e)
        {
            _locked = !_locked;
            _ctxMenu.MenuItems[1].Checked = _locked;
        }

        private void ctx_quickSave(object sender, EventArgs e)
        {
            if (_saved) return;

            if (!Directory.Exists(Properties.Settings.Default.DefaultPath))
                Directory.CreateDirectory(Properties.Settings.Default.DefaultPath);

            var path = Path.GetFullPath($"{_filePath}/{_fileName}.png");

            Screenshot.Save(path, ImageFormat.Png);

            var screenshotClone = (Bitmap)Screenshot.Clone();
            Screenshot.Dispose();
            picturePreview.Image.Dispose();
            Screenshot = screenshotClone;
            picturePreview.Image = Screenshot;

            _saved = true;

            UpdateContextMenu();
        }

        private void ctx_saveToDisk(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                FileName = _fileName,
                DefaultExt = "png"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var path = Path.GetFullPath(dialog.FileName);
            Screenshot.Save(path);


            var screenshotClone = (Bitmap)Screenshot.Clone();
            Screenshot.Dispose();
            picturePreview.Image.Dispose();
            Screenshot = screenshotClone;
            picturePreview.Image = Screenshot;


            _saved = true;
        }

        private void ctx_copyToClipboard(object sender, EventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetImage(Screenshot);
        }

        private void ctx_alwaysOnTopClick(object sender, EventArgs e)
        {
            _alwaysOnTop = !_alwaysOnTop;
            TopMost = _alwaysOnTop;
            _ctxMenu.MenuItems[0].Checked = _alwaysOnTop;
        }

        private void ctx_exit(object sender, EventArgs e)
        {
            Close();
        }

        private void picturePreview_MouseMove(object sender, MouseEventArgs e)
        {
            if (_locked) return;
            if (!_mouseIsDown) return;

            // Get the difference between the two points
            var xDiff = _startPoint.X - e.Location.X;
            var yDiff = _startPoint.Y - e.Location.Y;

            // Set the new point
            var x = Location.X - xDiff;
            var y = Location.Y - yDiff;

            Location = new Point(x, y);
        }

        private void picturePreview_MouseDown(object sender, MouseEventArgs e)
        {
            if (_locked) return;
            if (e.Button != MouseButtons.Left) return;

            _mouseIsDown = true;
            _startPoint = new Point(e.X, e.Y);
        }

        private void picturePreview_MouseUp(object sender, MouseEventArgs e)
        {
            if (_locked) return;
            _mouseIsDown = false;
        }
    }
}
