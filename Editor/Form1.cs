using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Editor
{
    public partial class Form1 : Form
    {
        private Bitmap _bitmap;

        public Form1(Bitmap bmp)
        {
            InitializeComponent();

            _bitmap = bmp;

            canvas.BackgroundImage = _bitmap;
            canvas.Location = new Point(100, 0);
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }

    class Canvas : Panel
    {
        public Canvas()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
        }
    }
}
