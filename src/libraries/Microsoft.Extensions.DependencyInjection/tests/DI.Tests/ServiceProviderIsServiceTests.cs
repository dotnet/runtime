// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Specification;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ServiceProviderIsServiceTests : ServiceProviderIsServiceSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection) => serviceCollection.BuildServiceProvider();
    }
}
