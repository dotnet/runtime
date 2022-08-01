// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using LightInject.Microsoft.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class LightInjectDependencyInjectionSpecificationTests : SkippableDependencyInjectionSpecificationTests
    {
        public override bool SupportsIServiceProviderIsService => false;

        public override string[] SkippedTests => new string[]
        {
            "NonSingletonService_WithInjectedProvider_ResolvesScopeProvider"
        };

        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            var builder = new ContainerBuilder();
            builder.Populate(serviceCollection);
            return serviceCollection.CreateLightInjectServiceProvider();
        }
    }
}
