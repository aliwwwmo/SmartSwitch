using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Diagnostics; 
using System.Threading; 
using System.Threading.Tasks;
using System.Net.NetworkInformation;
//using WpfAnimatedGif;
using System.Windows.Media.Imaging;

namespace SmartSwitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadNetworkAdapters();
            networkSwitcher.OnLog += LogMessage;
            initialConnectedAdapter = checker.GetConnectedAdapter();


        }
        private const double AspectRatio = 800.0 / 450.0; // نسبت عرض به ارتفاع اصلی
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                // وقتی عرض تغییر کرد، ارتفاع را براساس نسبت تنظیم کن
                Height = e.NewSize.Width / AspectRatio;
            }
            else if (e.HeightChanged)
            {
                // وقتی ارتفاع تغییر کرد، عرض را براساس نسبت تنظیم کن
                Width = e.NewSize.Height * AspectRatio;
            }
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            var mediaElement = sender as MediaElement;
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        }

        private IpChecker ipChecker = new IpChecker(); // مقداردهی به ipChecker
        NetworkSwitcher networkSwitcher = new NetworkSwitcher();
        NetworkTypeChecker checker = new NetworkTypeChecker();

        private void LoadNetworkAdapters()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in interfaces)
            {
                // بررسی وضعیت عملیاتی اداپتور
                if (networkInterface.OperationalStatus != OperationalStatus.NotPresent)
                {
                    NetworkAdapterComboBox1.Items.Add(networkInterface.Name);
                    NetworkAdapterComboBox2.Items.Add(networkInterface.Name);
                }
            }
        }
        private string initialConnectedAdapter;
        private async Task ResetToInitialState()
        {
            try
            {
                // ریست کردن UI
                StartButton.Background = new SolidColorBrush(Colors.Red);
                var glowEllipse = StartButton.Template.FindName("GlowEllipse", StartButton) as Ellipse;
                if (glowEllipse != null)
                {
                    glowEllipse.Fill = new SolidColorBrush(Colors.Red);
                }

                // پاک کردن متن‌های نمایشی
                StatusText.Text = "";
                PingText.Text = "";
                ActiveConnectionText.Text = "";

                // پاک کردن لاگ‌ها
                LogList.Items.Clear();
                LogMessage("برنامه به حالت اولیه برگشت.");

                // غیرفعال کردن آداپتور فعلی
                string currentAdapter = checker.GetConnectedAdapter();
                if (!string.IsNullOrEmpty(currentAdapter))
                {
                    await networkSwitcher.DisableNetworkInterfaceAsync(currentAdapter);
                }

                // فعال کردن آداپتور اولیه
                if (!string.IsNullOrEmpty(initialConnectedAdapter))
                {
                    await networkSwitcher.EnableNetworkInterfaceAsync(initialConnectedAdapter);
                    LogMessage($"اتصال به آداپتور اولیه {initialConnectedAdapter} برقرار شد.");
                }

                // ریست کردن ComboBox‌ها
                NetworkAdapterComboBox1.SelectedIndex = -1;
                NetworkAdapterComboBox2.SelectedIndex = -1;

                // ریست کردن متغیرهای وضعیت
                isRunning = false;
                ipChecker.IsCatchExecuted = false;
            }
            catch (Exception ex)
            {
                LogMessage($"خطا در بازگشت به حالت اولیه: {ex.Message}");
            }
        }
        private bool isRunning = false;

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
           
                // پیدا کردن GlowEllipse
                var glowEllipse = button.Template.FindName("GlowEllipse", button) as Ellipse;
                
                    // تغییر رنگ GlowEllipse
                    
                

            if (isRunning)
            {
                isRunning = false;
                StartButton.Background = new SolidColorBrush(Colors.Red);
                glowEllipse.Fill = new SolidColorBrush(Colors.Red); // رنگ جدید
                LogMessage("Operation Stopped.");
                await ResetToInitialState();

                return;
            }
            else
            {
                LogMessage("Starting Operation...");
                
                string netadap1 = NetworkAdapterComboBox1.Text;
                string netadap2 = NetworkAdapterComboBox2.Text;
                networkSwitcher.padap = netadap1;
                networkSwitcher.badap = netadap2;
                if (networkSwitcher.padap == "" || networkSwitcher.badap == "")
                {
                    MessageBox.Show("Please Select Network Adapters.");
                    isRunning = false;
                    return;
                }
                else
                {
                  glowEllipse.Fill = new SolidColorBrush(Colors.Green); // رنگ جدید
                    isRunning = true;
                    StartButton.Background = new SolidColorBrush(Colors.White);
                    LogMessage("Operation is Running...");
                    await networkSwitcher.DisableNetworkInterfaceAsync(checker.GetConnectedAdapter());
                    bool connectionSuccess = await networkSwitcher.EnableNetworkInterfaceAsync(netadap1);

                    if (!connectionSuccess)
                    {
                        LogMessage($"نتوانست به {netadap1} متصل شود. در حال تلاش مجدد...");
                        // می‌تونید اینجا منطق retry یا switching به آداپتور دیگه رو اضافه کنید
                        await networkSwitcher.EnableNetworkInterfaceAsync(netadap2);
                    }

                    //Thread.Sleep(5000);


                    while (isRunning)
                    {
                        string connectionType = checker.GetConnectedAdapter();
                        if (connectionType.Length > 17)
                        {
                            StatusText.Text = connectionType.Substring(0, 17) + "...";
                        }
                        else
                        {
                            StatusText.Text = connectionType;
                        }
                        string ping_out = ping.PingHelper.PingIp();
                       
                            
                        
                        if (ping_out.StartsWith("Error") || ping_out == "Unable to Ping")
                        {
                               ipChecker.IsCatchExecuted = false;

                            // فراخوانی نسخه غیرهمزمان سوئیچ شبکه
                            await networkSwitcher.SwitchNetworkConnectionAsync();

                            LogMessage("Operation stopped due to an error. Switching network...");

                            await Task.Delay(500); // تأخیر اضافی برای اطمینان از پایداری
                        }
                        else
                        {
                            if (ping.PingHelper.PingIp().Length > 4)
                            {
                                PingText.Text = ping.PingHelper.PingIp().Substring(0, 4) + "...";
                            }
                            else
                            {
                                PingText.Text = ping.PingHelper.PingIp();
                            }
                            string ActiveConnectionTextString = await ipChecker.GetPublicIpAsync();
                            if (ActiveConnectionTextString.Length > 15)
                            {
                                ActiveConnectionText.Text = ActiveConnectionTextString.Substring(0, 15) + "...";

                            }
                            else
                            {
                                ActiveConnectionText.Text = ActiveConnectionTextString;
                            }


                            LogMessage($"Ping: {ping.PingHelper.PingIp()}, IP: {ActiveConnectionTextString}");
                            await Task.Delay(1000);

                        }
                    }


                }



            }
           
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                // اضافه کردن پیام جدید
                LogList.Items.Add($"{DateTime.Now:HH:mm:ss} - {message}");

                // حذف لاگ‌های قدیمی اگر تعداد از حد مجاز بیشتر شده
                while (LogList.Items.Count > 12)
                {
                    LogList.Items.RemoveAt(0);
                }

                // اسکرول به آخرین پیام
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            });
        }
    }
}