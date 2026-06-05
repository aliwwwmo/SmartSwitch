using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartSwitch
{
    internal class IpChecker
    {
        // استفاده از یک نمونه ثابت (Static) برای جلوگیری از Socket Exhaustion
        private static readonly HttpClient _httpClient = new HttpClient();

        private bool _isCatchExecuted = false;

        public bool IsCatchExecuted
        {
            get { return _isCatchExecuted; }
            set { _isCatchExecuted = value; }
        }

        public async Task<string> GetPublicIpAsync()
        {
            try
            {
                // تنظیم هدرها روی همان کلاینت استاتیک (بدون ساخت کلاینت جدید)
                _httpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true
                };

                // استفاده از Guid برای جلوگیری از کش شدن نتیجه در سرور یا ISP
                string randomParam = Guid.NewGuid().ToString();

                string ip = await _httpClient.GetStringAsync($"https://api.ipify.org?random={randomParam}");

                return ip;
            }
            catch (Exception ex)
            {
                _isCatchExecuted = true;
                return $"Error: {ex.Message}";
            }
        }
    }
}