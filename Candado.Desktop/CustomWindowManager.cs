using Caliburn.Micro;
using System.Windows;

namespace Candado.Desktop
{
    public class CustomWindowManager : WindowManager
    {
        protected override Window EnsureWindow(object model, object view, bool isDialog)
        {
            var window = base.EnsureWindow(model, view, isDialog);

            window.SizeToContent = SizeToContent.Manual;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.ResizeMode = ResizeMode.CanResizeWithGrip;

            window.Height = 450;
            window.Width = 450;

            return window;
        }
    }
}