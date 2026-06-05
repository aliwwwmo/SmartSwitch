using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;

public class NetworkSwitcher
{
    private string Badap;
    private string Padap;
    // تعریف delegate برای لاگینگ
    public delegate void LogHandler(string message);
    // رویداد برای لاگینگ
    public event LogHandler OnLog;

    public string badap
    {
        get { return Badap; }
        set { Badap = value; }
    }
    public string padap
    {
        get { return Padap; }
        set { Padap = value; }
    }

    private void Log(string message)
    {
        OnLog?.Invoke(message);
    }

    public async void SwitchNetworkConnection()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        var activePadap = interfaces.FirstOrDefault(i =>
            i.Name == Padap && i.OperationalStatus == OperationalStatus.Up);
        var activeBadap = interfaces.FirstOrDefault(i =>
            i.Name == Badap && i.OperationalStatus == OperationalStatus.Up);

        if (activePadap != null)
        {
            Log($" فعال است: {Padap}");
            DisableNetworkInterface(Padap);
            EnableNetworkInterface(Badap);
            Log("سوئیچ انجام شد.");
        }
        else if (activeBadap != null)
        {
            Log($" فعال است: {Badap}");
            DisableNetworkInterface(Badap);
            EnableNetworkInterface(Padap);
            Log("سوئیچ انجام شد.");
        }
        else
        {
            Log("هیچ شبکه‌ای فعال نیست.");
        }
    }

    public void DisableNetworkInterface(string interfaceName)
    {
        ExecuteNetshCommand($"interface set interface \"{interfaceName}\" disable");
        Log($"رابط {interfaceName} غیرفعال شد.");
    }

    public void EnableNetworkInterface(string interfaceName)
    {
        ExecuteNetshCommand($"interface set interface \"{interfaceName}\" enable");
        Log($"رابط {interfaceName} فعال شد.");
    }

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
    public async Task<bool> EnableNetworkInterfaceAsync(string interfaceName)
    {
        ExecuteNetshCommand($"interface set interface \"{interfaceName}\" enable");
        Log($"رابط {interfaceName} فعال شد.");

        // انتظار برای اتصال کامل شبکه
        int maxAttempts = 10; // حداکثر 10 ثانیه انتظار
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = interfaces.FirstOrDefault(i => i.Name == interfaceName);

            if (networkInterface != null &&
                networkInterface.OperationalStatus == OperationalStatus.Up &&
                HasValidIpAddress(networkInterface))
            {
                Log($"رابط {interfaceName} با موفقیت متصل شد.");
                return true;
            }

            await Task.Delay(1000); // هر ثانیه چک کن
            attempts++;
            Log($"در حال انتظار برای اتصال {interfaceName}... تلاش {attempts}");
        }

        Log($"اتصال {interfaceName} با مشکل مواجه شد.");
        return false;
    }

    private bool HasValidIpAddress(NetworkInterface networkInterface)
    {
        var ipProperties = networkInterface.GetIPProperties();
        return ipProperties.UnicastAddresses.Any(addr =>
            addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
            !addr.Address.ToString().StartsWith("169.254")); // آدرس‌های APIPA را رد می‌کند
    }
    public async Task DisableNetworkInterfaceAsync(string interfaceName)
    {
        ExecuteNetshCommand($"interface set interface \"{interfaceName}\" disable");
        Log($"رابط {interfaceName} غیرفعال شد.");

        // منتظر شوید تا کارت شبکه غیرفعال شود
        while (true)
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = interfaces.FirstOrDefault(i => i.Name == interfaceName);
            if (networkInterface == null || networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                Log($"رابط {interfaceName} اکنون غیرفعال است.");
                break;
            }
            await Task.Delay(500); // بررسی هر نیم ثانیه
        }
    }
    public async Task SwitchNetworkConnectionAsync()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        var activePadap = interfaces.FirstOrDefault(i =>
            i.Name == Padap && i.OperationalStatus == OperationalStatus.Up);
        var activeBadap = interfaces.FirstOrDefault(i =>
            i.Name == Badap && i.OperationalStatus == OperationalStatus.Up);

        if (activePadap != null)
        {
            Log($" فعال است: {Padap}");
            await DisableNetworkInterfaceAsync(Padap);
            await EnableNetworkInterfaceAsync(Badap);
            Log("سوئیچ به انجام شد.");
        }
        else if (activeBadap != null)
        {
            Log($"Wi-Fi فعال است: {Badap}");
            await DisableNetworkInterfaceAsync(Badap);
            await EnableNetworkInterfaceAsync(Padap);
            Log("سوئیچ انجام شد.");
        }
        else
        {
            Log("هیچ شبکه‌ای فعال نیست.");
        }
    }


}