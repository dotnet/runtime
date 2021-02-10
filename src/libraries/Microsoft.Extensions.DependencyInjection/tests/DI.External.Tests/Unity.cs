// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class UnityDependencyInjectionSpecificationTests: SkippableDependencyInjectionSpecificationTests
    {
        // See https://github.com/unitycontainer/microsoft-dependency-injection/issues/87
        public override bool ExpectStructWithPublicDefaultConstructorInvoked => true;

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
