// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class GraceDependencyInjectionSpecificationTests : SkippableDependencyInjectionSpecificationTests
    {
        public override bool SupportsIServiceProviderIsService => false;

        public override string[] SkippedTests => new[]
        {
            "ResolvesMixedOpenClosedGenericsAsEnumerable",
            "ServiceScopeFactoryIsSingleton",
            "TypeActivatorWorksWithCtorWithOptionalArgs_WithStructDefaults"
        };

        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            return new DependencyInjectionContainer().Populate(serviceCollection);
        }
    }
}
