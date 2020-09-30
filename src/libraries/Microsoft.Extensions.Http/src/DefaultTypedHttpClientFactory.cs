// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http
{
    internal class DefaultTypedHttpClientFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient> :
        ITypedHttpClientFactory<TClient>
    {
        private readonly Cache _cache;
        private readonly IServiceProvider _services;

        public DefaultTypedHttpClientFactory(Cache cache, IServiceProvider services)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _cache = cache;
            _services = services;
        }

        public TClient CreateClient(HttpClient httpClient)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            return (TClient)_cache.Activator(_services, new object[] { httpClient });
        }

        // The Cache should be registered as a singleton, so it that it can
        // act as a cache for the Activator. This allows the outer class to be registered
        // as a transient, so that it doesn't close over the application root service provider.
        public class Cache
        {
            private static readonly Func<ObjectFactory> _createActivator = () => ActivatorUtilities.CreateFactory(typeof(TClient), new Type[] { typeof(HttpClient), });

            private ObjectFactory _activator;
            private bool _initialized;
            private object _lock;

            public ObjectFactory Activator => LazyInitializer.EnsureInitialized(
                ref _activator,
                ref _initialized,
                ref _lock,
                _createActivator);
        }
    }
}
