using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading; 

namespace SmartSwitch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private loading loadingScreen;
        private MainWindow mainWindow;
        private DispatcherTimer timer; 

        protected override void OnStartup(StartupEventArgs e)
        {
            //base.OnStartup(e);

            //loadingScreen = new loading();
            //loadingScreen.Show();

            //// ساخت MainWindow
            mainWindow = new MainWindow();
            mainWindow.Visibility = Visibility.Visible; 

            //// تنظیم تایمر برای باز کردن MainWindow بعد از 5 ثانیه
            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(5); 
            //timer.Tick += Timer_Tick; 
            //timer.Start(); // شروع تایمر
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //timer.Stop(); 
            //loadingScreen.Close(); 
            //mainWindow.Show(); // نمایش پنجره اصلی
        }
    }
}
