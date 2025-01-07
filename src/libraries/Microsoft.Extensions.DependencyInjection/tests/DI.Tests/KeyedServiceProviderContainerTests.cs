// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Specification;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class KeyedServiceProviderDefaultContainerTests : KeyedDependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider(ServiceProviderMode.Default);
    }

    public class KeyedServiceProviderDynamicContainerTests : KeyedDependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider();
    }

    public class KeyedServiceProviderExpressionContainerTests : KeyedDependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider(ServiceProviderMode.Expressions);
    }

    public class KeyedServiceProviderILEmitContainerTests : KeyedDependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider(ServiceProviderMode.ILEmit);
    }
}
