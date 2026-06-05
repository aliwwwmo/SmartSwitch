using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SmartSwitch
{
    public class NetworkSwitcher
    {
        private string _badap;
        private string _padap;

        // تعریف delegate برای لاگینگ
        public delegate void LogHandler(string message);
        public event LogHandler OnLog;

        public string badap
        {
            get { return _badap; }
            set { _badap = value; }
        }
        public string padap
        {
            get { return _padap; }
            set { _padap = value; }
        }

        private void Log(string message)
        {
            OnLog?.Invoke(message);
        }

        // وارد کردن تابع بومی ویندوز برای پاکسازی آنی کش DNS
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern void DnsFlushResolverCache();

        // متد پایه برای اجرای دستورات در پس‌زمینه
        private void ExecuteNetshCommand(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
        }

        // تغییر اولویت شبکه (جایگزین Enable/Disable)
        public async Task SetAdapterMetricAsync(string interfaceName, int metricValue)
        {
            await Task.Run(() =>
            {
                ExecuteNetshCommand($"interface ipv4 set interface \"{interfaceName}\" metric={metricValue}");
                Log($"اولویت رابط {interfaceName} روی {metricValue} تنظیم شد.");
            });
        }

        // برگرداندن تنظیمات شبکه به حالت اتوماتیک ویندوز (برای زمان خروج از برنامه)
        public async Task ResetAdapterMetricAsync(string interfaceName)
        {
            await Task.Run(() =>
            {
                ExecuteNetshCommand($"interface ipv4 set interface \"{interfaceName}\" metric=auto");
                Log($"تنظیمات اولویت رابط {interfaceName} به حالت اتوماتیک برگشت.");
            });
        }

        // پاکسازی فوق سریع کش DNS
        public void FlushDns()
        {
            try
            {
                DnsFlushResolverCache();
                Log("کش DNS فوراً پاکسازی شد (Native API).");
            }
            catch (Exception ex)
            {
                Log($"خطا در پاکسازی کش DNS: {ex.Message}");
            }
        }

        // سوییچ همزمان و فوق سریع بین آداپتور اصلی و رزرو
        public async Task SwitchToAdapterAsync(string targetAdapter, string backupAdapter)
        {
            Log($"تغییر مسیر به: {targetAdapter}");

            // اجرای همزمان هر دو دستور برای کاهش ۵۰ درصدی زمان سوییچ
            var disableTask = SetAdapterMetricAsync(backupAdapter, 100);
            var enableTask = SetAdapterMetricAsync(targetAdapter, 10);

            // منتظر می‌مانیم تا هر دو با هم تمام شوند
            await Task.WhenAll(disableTask, enableTask);

            // پاکسازی آنی DNS
            FlushDns();
        }
    }
}