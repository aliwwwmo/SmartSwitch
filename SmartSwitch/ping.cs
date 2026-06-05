using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;


namespace SmartSwitch
{
    internal class ping
    {
        public static class PingHelper
        {
            public static string PingIp()
            {
                string ipAddress = "8.8.8.8"; // آدرس IP ثابت
                Ping pingSender = new Ping();
                try
                {
                    PingReply reply = pingSender.Send(ipAddress);

                    if (reply.Status == IPStatus.Success)
                    {
                        return $"{reply.RoundtripTime}ms";
                    }
                    else
                    {
                        return "Unable to Ping";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error : {ex.Message}";
                }
            }
        
        }

    }
}
