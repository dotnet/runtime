// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal abstract class ServiceProviderEngine
    {
        public abstract ServiceFactory RealizeService(ServiceCallSite callSite);
    }

    internal abstract class ServiceFactory
    {
        internal static ConstantServiceFactory FromValue(object instance) => new(instance);

        internal static DelegateServiceFactory FromFactory(Func<ServiceProviderEngineScope, object> factory) => new(factory);

        internal static NullServiceFactory Null { get; } = new();

        public abstract object Create(ServiceProviderEngineScope scope);

        internal sealed class DelegateServiceFactory : ServiceFactory
        {
            private readonly Func<ServiceProviderEngineScope, object> _factory;

            public DelegateServiceFactory(Func<ServiceProviderEngineScope, object> factory)
            {
                _factory = factory;
            }

            public override object Create(ServiceProviderEngineScope scope) => _factory(scope);
        }

        internal sealed class NullServiceFactory : ServiceFactory
        {
            public override object Create(ServiceProviderEngineScope scope) => null;
        }

        internal sealed class ConstantServiceFactory : ServiceFactory
        {
            private readonly object _value;
            public ConstantServiceFactory(object value)
            {
                _value = value;
            }

            public override object Create(ServiceProviderEngineScope scope) => _value;
        }
    }
}
