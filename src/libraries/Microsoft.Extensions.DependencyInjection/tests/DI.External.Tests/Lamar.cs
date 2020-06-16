// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
