using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmartSwitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double AspectRatio = 800.0 / 450.0;
        private IpChecker ipChecker = new IpChecker();
        private NetworkSwitcher networkSwitcher = new NetworkSwitcher();
        private NetworkTypeChecker checker = new NetworkTypeChecker();

        private string initialConnectedAdapter;
        private bool isRunning = false;
        private string _lastLogMessage = "";
        // متغیر جدید برای رهگیری اینکه در حال حاضر کدام آداپتور مسیر اصلی است
        private string currentActiveAdapter;

        public MainWindow()
        {
            InitializeComponent();
            LoadNetworkAdapters();
            networkSwitcher.OnLog += LogMessage;
            initialConnectedAdapter = checker.GetConnectedAdapter();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                Height = e.NewSize.Width / AspectRatio;
            }
            else if (e.HeightChanged)
            {
                Width = e.NewSize.Height * AspectRatio;
            }
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            var mediaElement = sender as MediaElement;
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        }

        private void LoadNetworkAdapters()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in interfaces)
            {
                if (networkInterface.OperationalStatus != OperationalStatus.NotPresent)
                {
                    NetworkAdapterComboBox1.Items.Add(networkInterface.Name);
                    NetworkAdapterComboBox2.Items.Add(networkInterface.Name);
                }
            }
        }

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

                StatusText.Text = "";
                PingText.Text = "";
                ActiveConnectionText.Text = "";
                LogList.Items.Clear();
                LogMessage("برنامه به حالت اولیه برگشت.");

                // برگرداندن تنظیمات اولویت هر دو آداپتور به حالت اتوماتیک پیش‌فرض ویندوز
                if (!string.IsNullOrEmpty(networkSwitcher.padap))
                    await networkSwitcher.ResetAdapterMetricAsync(networkSwitcher.padap);

                if (!string.IsNullOrEmpty(networkSwitcher.badap))
                    await networkSwitcher.ResetAdapterMetricAsync(networkSwitcher.badap);

                NetworkAdapterComboBox1.SelectedIndex = -1;
                NetworkAdapterComboBox2.SelectedIndex = -1;
                isRunning = false;
                ipChecker.IsCatchExecuted = false;
            }
            catch (Exception ex)
            {
                LogMessage($"خطا در بازگشت به حالت اولیه: {ex.Message}");
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var glowEllipse = button.Template.FindName("GlowEllipse", button) as Ellipse;

            if (isRunning)
            {
                isRunning = false;
                StartButton.Background = new SolidColorBrush(Colors.Red);
                glowEllipse.Fill = new SolidColorBrush(Colors.Red);
                LogMessage("عملیات متوقف شد.");
                await ResetToInitialState();
                return;
            }
            else
            {
                LogMessage("در حال شروع عملیات...");

                string netadap1 = NetworkAdapterComboBox1.Text;
                string netadap2 = NetworkAdapterComboBox2.Text;
                networkSwitcher.padap = netadap1;
                networkSwitcher.badap = netadap2;

                if (string.IsNullOrEmpty(networkSwitcher.padap) || string.IsNullOrEmpty(networkSwitcher.badap))
                {
                    MessageBox.Show("لطفاً آداپتورهای شبکه را انتخاب کنید.");
                    return;
                }

                glowEllipse.Fill = new SolidColorBrush(Colors.Green);
                isRunning = true;
                StartButton.Background = new SolidColorBrush(Colors.White);
                LogMessage("برنامه در حال اجراست...");

                // تغییر مسیر اولیه: netadap1 به عنوان اصلی و netadap2 به عنوان پشتیبان تنظیم می‌شود
                await networkSwitcher.SwitchToAdapterAsync(netadap1, netadap2);
                currentActiveAdapter = netadap1;

                while (isRunning)
                {
                    // نمایش نام آداپتور متصل
                    string connectionType = checker.GetConnectedAdapter();
                    StatusText.Text = connectionType.Length > 17 ? connectionType.Substring(0, 17) + "..." : connectionType;

                    // فقط یک بار تابع پینگ را صدا می‌زنیم تا منابع سیستم هدر نرود
                    string ping_out = await ping.PingHelper.PingIpAsync();

                    if (ping_out.StartsWith("Error") || ping_out == "Unable to Ping")
                    {
                        ipChecker.IsCatchExecuted = false;
                        LogMessage("ارتباط قطع شد! در حال سوییچ فوری مسیر شبکه...");

                        // سوییچ سریع به مسیر جایگزین
                        if (currentActiveAdapter == netadap1)
                        {
                            await networkSwitcher.SwitchToAdapterAsync(netadap2, netadap1);
                            currentActiveAdapter = netadap2;
                        }
                        else
                        {
                            await networkSwitcher.SwitchToAdapterAsync(netadap1, netadap2);
                            currentActiveAdapter = netadap1;
                        }

                        await Task.Delay(500); // تاخیر کوتاه برای پایداری پس از سوییچ
                    }
                    else
                    {
                        PingText.Text = ping_out.Length > 4 ? ping_out.Substring(0, 4) + "..." : ping_out;

                        string activeIp = await ipChecker.GetPublicIpAsync();
                        ActiveConnectionText.Text = activeIp.Length > 15 ? activeIp.Substring(0, 15) + "..." : activeIp;

                        LogMessage($"Ping: {ping_out}, IP: {activeIp}");
                        await Task.Delay(1000);
                    }
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // منطق دکمه استاپ را در صورت نیاز اینجا قرار دهید
        }

        private void LogMessage(string message)
        {
            // جلوگیری از اسپم شدن لاگ: اگر پیام دقیقاً مشابه پیام قبلی است، آن را نادیده بگیر
            if (message == _lastLogMessage) return;
            _lastLogMessage = message;

            Dispatcher.Invoke(() =>
            {
                // اضافه کردن پیام جدید
                LogList.Items.Add($"{DateTime.Now:HH:mm:ss} - {message}");

                // بهینه‌سازی حافظه: نگه داشتن فقط 20 لاگ آخر و پاک کردن قدیمی‌ترها
                while (LogList.Items.Count > 20)
                {
                    LogList.Items.RemoveAt(0);
                }

                // اسکرول نرم به آخرین پیام
                if (LogList.Items.Count > 0)
                {
                    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
                }
            });
        }
    }
}