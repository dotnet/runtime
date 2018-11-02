// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class TypeWithDefaultConstructorParameters
    {
        public TypeWithDefaultConstructorParameters(
            IFakeMultipleService multipleService,
            IFakeService fakeService = null)
        {
        }

        public TypeWithDefaultConstructorParameters(
            IFactoryService factoryService)
        {
        }

        public TypeWithDefaultConstructorParameters(
            IFactoryService factoryService,
            IFakeScopedService singletonService = null)
        {
        }
    }
}
