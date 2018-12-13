    // Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using StructureMap;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class StructureMapDependencyInjectionSpecificationTests: SkippableDependencyInjectionSpecificationTests
    {
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
