// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class AutofacDependencyInjectionSpecificationTests : DependencyInjectionSpecificationTests
    {
        public override bool SupportsIServiceProviderIsService => false;

        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            var builder = new ContainerBuilder();
            builder.Populate(serviceCollection);
            return new AutofacServiceProvider(builder.Build());
        }
    }
}
