// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ServiceProviderEngineScope : IServiceScope, IServiceProvider
    {
        // For testing only
        internal Action<object> _captureDisposableCallback;

        private List<IDisposable> _disposables;

        private bool _disposed;

        public ServiceProviderEngineScope(ServiceProviderEngine engine)
        {
            Engine = engine;
        }

        internal Dictionary<object, object> ResolvedServices { get; } = new Dictionary<object, object>();

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

        public void Dispose()
        {
            lock (ResolvedServices)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (_disposables != null)
                {
                    for (var i = _disposables.Count - 1; i >= 0; i--)
                    {
                        var disposable = _disposables[i];
                        disposable.Dispose();
                    }

                    _disposables.Clear();
                }

                ResolvedServices.Clear();
            }
        }

        internal object CaptureDisposable(object service)
        {
            _captureDisposableCallback?.Invoke(service);

            if (!ReferenceEquals(this, service))
            {
                if (service is IDisposable disposable)
                {
                    lock (ResolvedServices)
                    {
                        if (_disposables == null)
                        {
                            _disposables = new List<IDisposable>();
                        }

                        _disposables.Add(disposable);
                    }
                }
            }
            return service;
        }
    }
}