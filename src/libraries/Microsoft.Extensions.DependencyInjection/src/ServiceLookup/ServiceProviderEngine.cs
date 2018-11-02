// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal abstract class ServiceProviderEngine : IServiceProviderEngine, IServiceScopeFactory
    {
        private readonly IServiceProviderEngineCallback _callback;

        private readonly Func<Type, Func<ServiceProviderEngineScope, object>> _createServiceAccessor;

        private bool _disposed;

        protected ServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProviderEngineCallback callback)
        {
            _createServiceAccessor = CreateServiceAccessor;
            _callback = callback;
            Root = new ServiceProviderEngineScope(this);
            RuntimeResolver = new CallSiteRuntimeResolver();
            CallSiteFactory = new CallSiteFactory(serviceDescriptors);
            CallSiteFactory.Add(typeof(IServiceProvider), new ServiceProviderCallSite());
            CallSiteFactory.Add(typeof(IServiceScopeFactory), new ServiceScopeFactoryCallSite());
            RealizedServices = new ConcurrentDictionary<Type, Func<ServiceProviderEngineScope, object>>();
        }

        internal ConcurrentDictionary<Type, Func<ServiceProviderEngineScope, object>> RealizedServices { get; }

        internal CallSiteFactory CallSiteFactory { get; }

        protected CallSiteRuntimeResolver RuntimeResolver { get; }

        public ServiceProviderEngineScope Root { get; }

        public IServiceScope RootScope => Root;

        public object GetService(Type serviceType) => GetService(serviceType, Root);

        protected abstract Func<ServiceProviderEngineScope, object> RealizeService(IServiceCallSite callSite);

        public void Dispose()
        {
            _disposed = true;
            Root.Dispose();
        }

        internal object GetService(Type serviceType, ServiceProviderEngineScope serviceProviderEngineScope)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            var realizedService = RealizedServices.GetOrAdd(serviceType, _createServiceAccessor);
            _callback?.OnResolve(serviceType, serviceProviderEngineScope);
            return realizedService.Invoke(serviceProviderEngineScope);
        }

        public IServiceScope CreateScope()
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            return new ServiceProviderEngineScope(this);
        }

        private Func<ServiceProviderEngineScope, object> CreateServiceAccessor(Type serviceType)
        {
            var callSite = CallSiteFactory.CreateCallSite(serviceType, new CallSiteChain());
            if (callSite != null)
            {
                _callback?.OnCreate(callSite);
                return RealizeService(callSite);
            }

            return _ => null;
        }
    }
}