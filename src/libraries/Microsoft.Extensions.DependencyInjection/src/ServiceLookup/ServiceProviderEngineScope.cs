// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ServiceProviderEngineScope : IServiceScope, IServiceProvider
#if DISPOSE_ASYNC
        , IAsyncDisposable
#endif
    {
        // For testing only
        internal Action<object> _captureDisposableCallback;

        private List<object> _disposables;

        private bool _disposed;

        public ServiceProviderEngineScope(ServiceProviderEngine engine)
        {
            Engine = engine;
        }

        internal Dictionary<ServiceCacheKey, object> ResolvedServices { get; } = new Dictionary<ServiceCacheKey, object>();

        public ServiceProviderEngine Engine { get; }

        public object GetService(Type serviceType)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            return Engine.GetService(serviceType, this);
        }

        public IServiceProvider ServiceProvider => this;

        internal object CaptureDisposable(object service)
        {
            Debug.Assert(!_disposed);

            _captureDisposableCallback?.Invoke(service);

            if (ReferenceEquals(this, service) ||
               !(service is IDisposable
#if DISPOSE_ASYNC
                || service is IAsyncDisposable
#endif
                ))
            {
                return service;
            }

            lock (ResolvedServices)
            {
                if (_disposables == null)
                {
                    _disposables = new List<object>();
                }

                _disposables.Add(service);
            }
            return service;
        }

        public void Dispose()
        {
            var toDispose = BeginDispose();

            if (toDispose != null)
            {
                for (var i = toDispose.Count - 1; i >= 0; i--)
                {
                    if (toDispose[i] is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else
                    {
                        throw new InvalidOperationException(Resources.FormatAsyncDisposableServiceDispose(TypeNameHelper.GetTypeDisplayName(toDispose[i])));
                    }
                }
            }
        }

#if DISPOSE_ASYNC
        public async ValueTask DisposeAsync()
        {
            var toDispose = BeginDispose();

            if (toDispose != null)
            {
                for (var i = toDispose.Count - 1; i >= 0; i--)
                {
                    var disposable = toDispose[i];
                    if (disposable is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else
                    {
                        ((IDisposable)disposable).Dispose();
                    }
                }
            }
        }
#endif

        private List<object> BeginDispose()
        {
            List<object> toDispose;
            lock (ResolvedServices)
            {
                if (_disposed)
                {
                    return null;
                }

                _disposed = true;
                toDispose = _disposables;
                _disposables = null;

                ResolvedServices.Clear();
            }

            return toDispose;
        }
    }
}
