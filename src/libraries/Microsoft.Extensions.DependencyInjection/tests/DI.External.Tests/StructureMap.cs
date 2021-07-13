// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using StructureMap;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class StructureMapDependencyInjectionSpecificationTests: SkippableDependencyInjectionSpecificationTests
    {
        public override bool SupportsIServiceProviderIsService => false;

        public override string[] SkippedTests => new[]
        {
            "DisposesInReverseOrderOfCreation",
            "ResolvesMixedOpenClosedGenericsAsEnumerable"
        };

        protected override IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection)
        {
            var container = new Container();
            container.Configure(config =>
            {
                config.Populate(serviceCollection);
            });

            return container.GetInstance<IServiceProvider>();
        }
    }
}
