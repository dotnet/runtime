// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
