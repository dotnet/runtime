// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class UnityDependencyInjectionSpecificationTests: SkippableDependencyInjectionSpecificationTests
    {
        public override string[] SkippedTests => new[]
        {
            "SingletonServiceCanBeResolvedFromScope"
        };

        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            return Unity.Microsoft.DependencyInjection.ServiceProviderExtensions.BuildServiceProvider(serviceCollection);
        }
    }
}
