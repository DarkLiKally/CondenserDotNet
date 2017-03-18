﻿using CondenserDotNet.Client;
using CondenserDotNet.Core;
using CondenserDotNet.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Condenser.Tests.Integration.Routing
{
    public class RoutingFixture : IDisposable
    {
        private Dictionary<string, ServiceInstance> _hosts = new Dictionary<string, ServiceInstance>();
        private const string HealthRoute = "/health";
        private volatile Dictionary<string, List<IService>> _currentRegistrations;
        private int routerPort;
        private AsyncManualResetEvent<bool> _wait = new AsyncManualResetEvent<bool>();
        private RoutingHost _host;
        private IWebHost _routerHost;
        private HttpClient _client = new HttpClient();

        public RoutingFixture()
        {

        }

        public void SetServiceHealth(string name, bool isHealthy)
        {
            _hosts[name].IsHealthy = isHealthy;
        }

        public string GetNewServiceName()
        {
            return Guid.NewGuid().ToString().Substring(0, 10);
        }

        public Task<HttpResponseMessage> CallRouterAsync(string route)
        {
            return _client.GetAsync($"http://localhost:{routerPort}" + route);
        }

        private void RegisterService(string name, int port, string route)
        {
            var options = Options.Create(new ServiceManagerConfig
            {
                ServiceName = name,
                ServicePort = port

            });

            var serviceManager = new ServiceManager(options);

            serviceManager
                .AddHttpHealthCheck(HealthRoute, 10)
                .AddApiUrl(route)
                .RegisterServiceAsync().Wait();
        }

        public RoutingFixture AddService(string name, string route)
        {
            var hostPort = ServiceManagerConfig.GetNextAvailablePort();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://*:{hostPort}")
                .Configure(app =>
                {
                    RegisterService(name, hostPort, route);

                    app.Run(async message =>
                    {
                        HttpStatusCode status;
                        string content = null;
                        var path = message.Request.Path;

                        var instance = _hosts[name];

                        if (path == HealthRoute)
                        {                            
                            if (instance.IsHealthy)
                            {
                                status = HttpStatusCode.OK;
                                content = "Healthy";
                            }
                            else
                            {
                                status = HttpStatusCode.InternalServerError;
                                content = "Not healthy";
                            }
                        }
                        else if (path == route)
                        {
                            status = HttpStatusCode.OK;
                            content = "Called me " + name;
                        }
                        else
                        {
                            status = HttpStatusCode.NotFound;
                            content = "";
                        }

                        message.Response.StatusCode = (int)status;
                        await message.Response.WriteAsync(content);

                    });
                })
                .Build();

            _hosts.Add(name, new ServiceInstance(host));

            return this;
        }

        public void AddRouter()
        {
            routerPort = ServiceManagerConfig.GetNextAvailablePort();

            _routerHost= new WebHostBuilder()
                .UseKestrel()
                .UseLoggerFactory(new LoggerFactory().AddConsole())
                .UseUrls($"http://*:{routerPort}")
                .ConfigureServices(x =>
                {
                    x.AddCondenserWithBuilder()
                    .Build();
                })
                .Configure(app =>
                {
                    _host = (RoutingHost)app.ApplicationServices.GetService(typeof(RoutingHost));
                    _host.OnRouteBuilt = SignalWhenAllRegistered;

                    app.UseCondenser();
                })
                .Build();

            
        }

        public Task<bool> WaitForRegistrationAsync()
        {
            return _wait.WaitAsync();
        }

        private void SignalWhenAllRegistered(Dictionary<string, List<IService>> data)
        {
            if (_hosts.All(h => data.Keys.Contains(h.Key)))
            {
                _wait.Set(true);
            }
            Interlocked.Exchange(ref _currentRegistrations, data);
        }

        public void StartAll()
        {
            foreach (var host in _hosts)
            {
                host.Value.Host.Start();
            }

            _routerHost?.Start();
        }

        public void Dispose()
        {
            foreach (var host in _hosts)
            {
                host.Value.Host.Dispose();
            }

            _routerHost?.Dispose();
        }

        public class ServiceInstance
        {
            public IWebHost Host { get; }

            public bool IsHealthy { get; set; }
            public ServiceInstance(IWebHost host)
            {
                Host = host;
                IsHealthy = true;
            }
        }
    }
}