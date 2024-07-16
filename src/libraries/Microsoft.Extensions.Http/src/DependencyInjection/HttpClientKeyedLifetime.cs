// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class HttpClientKeyedLifetime
    {
        public static readonly HttpClientKeyedLifetime Disabled = new(null!, null!, null!);

        public object ServiceKey { get; }
        public ServiceDescriptor Client { get; }
        public ServiceDescriptor Handler { get; }

        public bool IsDisabled => ReferenceEquals(this, Disabled);

        private HttpClientKeyedLifetime(object serviceKey, ServiceDescriptor client, ServiceDescriptor handler)
        {
            ServiceKey = serviceKey;
            Client = client;
            Handler = handler;
        }

        private HttpClientKeyedLifetime(object serviceKey, ServiceLifetime lifetime)
        {
            ThrowHelper.ThrowIfNull(serviceKey);
            ServiceKey = serviceKey;
            Client = ServiceDescriptor.DescribeKeyed(typeof(HttpClient), ServiceKey, CreateKeyedClient, lifetime);
            Handler = ServiceDescriptor.DescribeKeyed(typeof(HttpMessageHandler), ServiceKey, CreateKeyedHandler, lifetime);
        }

        public HttpClientKeyedLifetime(ServiceLifetime lifetime) : this(KeyedService.AnyKey, lifetime) { }
        public HttpClientKeyedLifetime(string name, ServiceLifetime lifetime) : this((object)name, lifetime) { }

        public void AddRegistration(IServiceCollection services)
        {
            if (IsDisabled)
            {
                return;
            }

            services.Add(Client);
            services.Add(Handler);
        }

        public void RemoveRegistration(IServiceCollection services)
        {
            if (IsDisabled)
            {
                return;
            }

            services.Remove(Client);
            services.Remove(Handler);
        }

        private static HttpClient CreateKeyedClient(IServiceProvider serviceProvider, object? key)
        {
            if (key is not string name || IsKeyedLifetimeDisabled(serviceProvider, name))
            {
                return null!;
            }
            return serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(name);
        }

        private static HttpMessageHandler CreateKeyedHandler(IServiceProvider serviceProvider, object? key)
        {
            if (key is not string name || IsKeyedLifetimeDisabled(serviceProvider, name))
            {
                return null!;
            }
            return serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
        }

        private static bool IsKeyedLifetimeDisabled(IServiceProvider serviceProvider, string name)
        {
            HttpClientMappingRegistry registry = serviceProvider.GetRequiredService<HttpClientMappingRegistry>();

            if (!registry.KeyedLifetimeMap.TryGetValue(name, out HttpClientKeyedLifetime? registration))
            {
                registration = registry.DefaultKeyedLifetime;
            }

            return registration?.IsDisabled ?? false;
        }
    }
}
