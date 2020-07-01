// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    internal static class ServiceCollectionContainerBuilderTestExtensions
    {
        public static ServiceProvider BuildServiceProvider(this IServiceCollection services, ServiceProviderMode mode)
        {
            IServiceProviderEngine engine = mode switch
            {
                ServiceProviderMode.Default =>
                    RuntimeFeature.IsDynamicCodeCompiled ?
                        (IServiceProviderEngine)new DynamicServiceProviderEngine(services) :
                        new RuntimeServiceProviderEngine(services),
                ServiceProviderMode.Dynamic => new DynamicServiceProviderEngine(services),
                ServiceProviderMode.Runtime => new RuntimeServiceProviderEngine(services),
                ServiceProviderMode.Expressions => new ExpressionsServiceProviderEngine(services),
                ServiceProviderMode.ILEmit => new ILEmitServiceProviderEngine(services),
                _ => throw new NotSupportedException()
            };

            return new ServiceProvider(services, engine, ServiceProviderOptions.Default);
        }
    }
}
