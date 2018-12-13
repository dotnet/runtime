    // Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using LightInject.Microsoft.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class LightInjectDependencyInjectionSpecificationTests: DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            var builder = new ContainerBuilder();
            builder.Populate(serviceCollection);
            return serviceCollection.CreateLightInjectServiceProvider();
        }
    }
}
