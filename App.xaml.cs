using System;
using System.Configuration;
using System.Data;
using System.IO;
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
            // 在访问任何 WeChatOcr 类型之前，先设置正确的工作目录
            InitializeWorkingDirectory();
            
            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "未处理异常");
                e.Handled = true;
            };
        }

        private static void InitializeWorkingDirectory()
        {
            try
            {
                // 获取程序运行目录，并设置为当前工作目录
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Directory.SetCurrentDirectory(baseDir);
            }
            catch
            {
                // 忽略异常
            }
        }
    }
}

