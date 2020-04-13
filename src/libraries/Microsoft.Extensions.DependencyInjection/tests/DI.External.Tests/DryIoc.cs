// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
