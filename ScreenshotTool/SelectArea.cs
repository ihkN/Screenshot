using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace ScreenshotTool
{
    public partial class SelectArea : Form
    {
        private const int j = 0x4A;
        private const int w = 0x57;

        public SelectArea()
        {
            InitializeComponent();

            //_screen = screen;
        }

        Point _startPos;                                         // mouse-down position
        Point _currentPos;                                       // current mouse position
        bool _drawing;                                           // busy drawing
        private readonly List<Rectangle> _rectangles = new List<Rectangle>();     // previous rectangles
        private readonly Canvas _canevas = new Canvas();

        private void Form1_Load(object sender, EventArgs e)
        {
            var totalSize = Screen.AllScreens.Aggregate(Rectangle.Empty, (current, s) => Rectangle.Union(current, s.Bounds));

            Width = totalSize.Width;
            Height = totalSize.Height;

            Left = totalSize.Left;
            Top = totalSize.Top;
        }

        private Bitmap TakeScreenshot(int x, int y, int width, int height)
        {
            Opacity = 0;
            var rect = new Rectangle(x, y, width, height);
            var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

            return bmp;
        }


        private Rectangle GetRectangle()
        {
            return new Rectangle(
                Math.Min(_startPos.X, _currentPos.X),
                Math.Min(_startPos.Y, _currentPos.Y),
                Math.Abs(_startPos.X - _currentPos.X),
                Math.Abs(_startPos.Y - _currentPos.Y));
        }

        private void canevas_MouseDown(object sender, MouseEventArgs e)
        {
            _currentPos = _startPos = e.Location;
            _drawing = true;
        }

        private void canevas_MouseMove(object sender, MouseEventArgs e)
        {
            _currentPos = e.Location;
            if (_drawing) _canevas.Invalidate();
            Refresh();
        }

        private void canevas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_drawing)
            {
                _drawing = false;
                var rc = GetRectangle();
                if (rc.Width > 0 && rc.Height > 0)
                {
                    _rectangles.Add(rc);

                    var preview = new PreviewArea(TakeScreenshot(rc.X, rc.Y, rc.Width, rc.Height), string.Empty);
                    preview.Show();
                    Hide();
                }
                _canevas.Invalidate();
            }
        }

        private void canevas_Paint(object sender, PaintEventArgs e)
        {
            var pen = new Pen(Color.Red, 3f);

            if (_rectangles.Count > 0) e.Graphics.DrawRectangles(pen, _rectangles.ToArray());
            if (_drawing) e.Graphics.DrawRectangle(pen, GetRectangle());
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.Close();
        }
    }
}
