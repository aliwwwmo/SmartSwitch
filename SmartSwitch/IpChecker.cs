using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartSwitch
{
    internal class IpChecker
    {
        private static readonly HttpClient client = new HttpClient();
        private bool isCatchExecuted = false;

        // پراپرتی عمومی برای دسترسی به وضعیت
        public bool IsCatchExecuted
        {
            get { return isCatchExecuted; }
            set { isCatchExecuted = value; }
        }

        public async Task<string> GetPublicIpAsync()
        {

            try
            {
                using (var client = new HttpClient()) // ایجاد یک نمونه جدید HttpClient
                {
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                    {
                        NoCache = true,
                        NoStore = true
                    };

                    string randomParam = Guid.NewGuid().ToString(); // پارامتر تصادفی
                    string ip = await client.GetStringAsync($"https://api.ipify.org?random={randomParam}");
                    return ip;
                }

            }
            catch (Exception ex)
            {
                isCatchExecuted = true;

                return $"Error: {ex.Message}";
            }
        }
    }
}
