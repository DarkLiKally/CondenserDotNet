using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CondenserDotNet.Client
{
    public static class MaintenanceExtensions
    {
        private const string _url = "/v1/agent/service/maintenance/";
               
        public static Task EnableMaintenanceModeAsync(this IServiceManager manager, string reason)
        {
            var url = _url + $"{manager.ServiceId}?enable=true&reason={Uri.EscapeDataString(reason)}";
            return manager.PutAsync(url);
        }

        public static Task DisableMaintenanceModeAsync(this IServiceManager manager)
        {
            var url = _url + $"{manager.ServiceId}?enable=false";
            return manager.PutAsync(url, string.Empty);
        }
    }
}
