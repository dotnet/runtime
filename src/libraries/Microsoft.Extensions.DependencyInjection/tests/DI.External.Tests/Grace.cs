    // Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class GraceDependencyInjectionSpecificationTests: SkippableDependencyInjectionSpecificationTests
    {
        public override string[] SkippedTests => new[]
        {
            "ResolvesMixedOpenClosedGenericsAsEnumerable",
            "TypeActivatorWorksWithCtorWithOptionalArgs_WithStructDefaults"
        };

        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            return new DependencyInjectionContainer().Populate(serviceCollection);
        }
    }
}
