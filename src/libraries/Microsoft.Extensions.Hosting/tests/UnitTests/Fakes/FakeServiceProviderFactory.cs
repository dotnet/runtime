// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Fakes
{
    public class FakeServiceProviderFactory : IServiceProviderFactory<FakeServiceCollection>
    {
        public FakeServiceCollection CreateBuilder(IServiceCollection services)
        {
            var container = new FakeServiceCollection();
            container.Populate(services);
            return container;
        }

        public IServiceProvider CreateServiceProvider(FakeServiceCollection containerBuilder)
        {
            containerBuilder.Build();
            return containerBuilder;
        }
    }
}
