// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class DryIocDependencyInjectionSpecificationTests : DependencyInjectionSpecificationTests
    {
        public override bool SupportsIServiceProviderIsService => false;

        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            return new Container()
                .WithDependencyInjectionAdapter(serviceCollection)
                .BuildServiceProvider();
        }
    }
}
