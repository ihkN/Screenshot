using System.Windows.Forms;

namespace ScreenshotTool
{
    class Canvas : Panel
    {
        public Canvas()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
        }
    }
}
