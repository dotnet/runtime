// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class DryIocDependencyInjectionSpecificationTests : SkippableDependencyInjectionSpecificationTests
    {
        public override bool SupportsIServiceProviderIsService => false;

        public override string[] SkippedTests => new[]
        {
            "ServiceScopeFactoryIsSingleton"
        };

        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            return new Container()
                .WithDependencyInjectionAdapter(serviceCollection)
                .BuildServiceProvider();
        }
    }
}
