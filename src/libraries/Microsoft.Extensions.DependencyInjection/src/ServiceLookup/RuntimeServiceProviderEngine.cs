// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class RuntimeServiceProviderEngine : ServiceProviderEngine
    {
        public static RuntimeServiceProviderEngine Instance { get; } = new RuntimeServiceProviderEngine();

        private RuntimeServiceProviderEngine() { }

        public override ServiceFactory RealizeService(ServiceCallSite callSite) =>
            new RuntimeServiceFactory(callSite);

        internal sealed class RuntimeServiceFactory : ServiceFactory
        {
            private readonly ServiceCallSite _serviceCallSite;

            public RuntimeServiceFactory(ServiceCallSite serviceCallSite)
            {
                _serviceCallSite = serviceCallSite;
            }

            public override object Create(ServiceProviderEngineScope scope) =>
                CallSiteRuntimeResolver.Instance.Resolve(_serviceCallSite, scope);
        }
    }
}
