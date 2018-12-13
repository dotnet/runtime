    // Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class LamarDependencyInjectionSpecificationTests : SkippableDependencyInjectionSpecificationTests
    {
        public override string[] SkippedTests => new[]
        {
            "DisposesInReverseOrderOfCreation",
            "ResolvesMixedOpenClosedGenericsAsEnumerable"
        };

        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            return Lamar.Container.BuildAsync(serviceCollection).Result;
        }
    }
}
