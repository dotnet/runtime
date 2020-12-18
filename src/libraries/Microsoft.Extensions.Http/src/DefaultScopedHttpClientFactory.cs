// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http
{
    internal class DefaultScopedHttpClientFactory : IScopedHttpClientFactory, IScopedHttpMessageHandlerFactory
    {
        private DefaultHttpClientFactory _factory;
        private IServiceProvider _services;
        private IOptionsMonitor<HttpClientFactoryOptions> _optionsMonitor;

        // cache for creating a chain only once per scope
        private ConcurrentDictionary<string, HttpMessageHandler> _cache = new ConcurrentDictionary<string, HttpMessageHandler>();

        public DefaultScopedHttpClientFactory(DefaultHttpClientFactory factory, IServiceProvider services, IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor)
        {
            _factory = factory;
            _services = services;
            _optionsMonitor = optionsMonitor;
        }

        public HttpClient CreateClient(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            HttpClientFactoryOptions options = _optionsMonitor.Get(name);
            if (!options.PreserveExistingScope || options.SuppressHandlerScope)
            {
                throw new Exception(); //todo
            }

            HttpMessageHandler handler = CreateHandler(name);
            var client = new HttpClient(handler);
            for (int i = 0; i < options.HttpClientActions.Count; i++)
            {
                options.HttpClientActions[i](client);
            }

            return client;
        }

        public HttpMessageHandler CreateHandler(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            HttpClientFactoryOptions options = _optionsMonitor.Get(name);
            if (!options.PreserveExistingScope || options.SuppressHandlerScope)
            {
                throw new Exception(); //todo
            }

            // thread safety of the `valueFactory` param for GetOrAdd is handled by _factory.CreateHandler
            return _cache.GetOrAdd(name, s => _factory.CreateHandler(s, _services));
        }
    }
}
