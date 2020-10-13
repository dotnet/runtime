// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    internal class DefaultScopedHttpClientFactory : IScopedHttpClientFactory
    {
        private DefaultHttpClientFactory _factory;
        private IServiceProvider _services;

        public DefaultScopedHttpClientFactory(DefaultHttpClientFactory factory, IServiceProvider services)
        {
            _factory = factory;
            _services = services;
        }

        public HttpClient CreateClient(string name)
        {
            return _factory.CreateClient(name, _services);
        }
    }
}
