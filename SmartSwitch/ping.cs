using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SmartSwitch
{
    internal class ping
    {
        public static class PingHelper
        {
            // متد جدید به صورت غیرهمزمان (Async) با تست TCP Socket
            public static async Task<string> PingIpAsync()
            {
                string host = "1.1.1.1"; // سرور کلودفلر (بسیار سریع‌تر و پایدارتر از گوگل)
                int port = 53;           // پورت DNS که همیشه روی سرورها باز است
                int timeoutMs = 800;     // تایم‌اوت 800 میلی‌ثانیه (برای تشخیص سریع‌تر قطعی)

                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    using (var tcpClient = new TcpClient())
                    {
                        stopwatch.Start();

                        // تلاش برای اتصال روی پورت مشخص با تایم‌اوت دستی
                        var connectTask = tcpClient.ConnectAsync(host, port);
                        var timeoutTask = Task.Delay(timeoutMs);

                        // بررسی اینکه آیا اتصال سریع‌تر از تایم‌اوت برقرار شد یا خیر
                        if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
                        {
                            stopwatch.Stop();
                            // اگر وصل شد، زمان را برمی‌گردانیم
                            return $"{stopwatch.ElapsedMilliseconds}ms";
                        }
                        else
                        {
                            // اگر تایم‌اوت شد، اتصال را می‌بندیم و خطا می‌دهیم
                            stopwatch.Stop();
                            tcpClient.Close();
                            return "Unable to Ping";
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
        }
    }
}