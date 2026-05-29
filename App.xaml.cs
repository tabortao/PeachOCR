using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace PeachOCR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_SHOW = 5;

        public App()
        {
            InitializeWorkingDirectory();

            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "未处理异常");
                e.Handled = true;
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0 && e.Args[0] != "gui" && e.Args[0] != "--gui")
            {
                AllocConsole();
                ShowWindow(GetConsoleWindow(), SW_SHOW);
                int exitCode = 0;
                try
                {
                    var cli = new CLI.PeachOcrCli();
                    exitCode = await cli.Run(e.Args);
                    Console.WriteLine();
                    Console.WriteLine($"[CLI退出码: {exitCode}]");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"CLI执行错误：{ex.Message}");
                    if (e.Args.Contains("--verbose") || e.Args.Contains("-v"))
                    {
                        Console.Error.WriteLine(ex.StackTrace);
                    }
                    exitCode = 1;
                }
                Console.WriteLine();
                Console.Write("按任意键退出...");
                Console.ReadKey(true);
                Environment.Exit(exitCode);
                return;
            }

            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private static void InitializeWorkingDirectory()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Directory.SetCurrentDirectory(baseDir);
            }
            catch
            {
            }
        }
    }
}
