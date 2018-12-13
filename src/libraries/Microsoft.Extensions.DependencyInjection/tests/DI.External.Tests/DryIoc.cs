    // Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class DryIocDependencyInjectionSpecificationTests: DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            return new Container()
                .WithDependencyInjectionAdapter(serviceCollection)
                .BuildServiceProvider();
        }
    }
}
