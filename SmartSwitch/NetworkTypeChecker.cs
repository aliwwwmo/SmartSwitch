using System;
using System.Linq;
using System.Net.NetworkInformation;

public class NetworkTypeChecker
{
    public string GetConnectedAdapter()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var adapter in networkInterfaces)
        {
            // بررسی وضعیت آداپتور
            if (adapter.OperationalStatus == OperationalStatus.Up)
            {
                var properties = adapter.GetIPProperties();

                // بررسی اینکه آداپتور دارای آدرس IP باشد
                if (properties.GatewayAddresses.Count > 0)
                {
                    return adapter.Name;
                }
            }
        }

        return "No connected adapter found.";
    }
}

