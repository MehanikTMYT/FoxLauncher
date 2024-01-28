using System;
using System.Windows;

namespace FoxLauncher
{
    public partial class App : System.Windows.Application
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
    }
}