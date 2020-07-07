// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
