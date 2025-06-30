using System.Configuration;
using System.Data;
using System.Windows;

namespace PeachOCR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "未处理异常");
                e.Handled = true;
            };
        }
    }
}

