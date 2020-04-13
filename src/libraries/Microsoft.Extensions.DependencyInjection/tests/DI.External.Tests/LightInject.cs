// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
