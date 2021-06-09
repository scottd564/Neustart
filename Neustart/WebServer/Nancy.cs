using Nancy.Hosting.Self;
using System;

namespace Neustart.APIServer
{
    class Nancy
    {
        public static NancyHost Start()
        {
            HostConfiguration hostconfig = new HostConfiguration()
            {
                RewriteLocalhost = false,
                AllowAuthorityFallback = true,
                UrlReservations = new UrlReservations()
                {
                    CreateAutomatically = true
                }
            };

            Uri[] uriAddresses = new Uri[] {
                new Uri(Core.Cfg.Web.BindAddressHTTP),
                new Uri(Core.Cfg.Web.BindAddressHTTPS)
            };

            NancyHost host = new NancyHost(new NancyBoostrap(), hostconfig, uriAddresses);
            host.Start();

            Debug.Log("Nancy Self-host started!");
            return host;
        }
    }
}