// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class TypeWithGenericServices
    {
        public TypeWithGenericServices(
            IFakeService fakeService,
            IFakeOpenGenericService<IFakeService> logger)
        {
        }

        public TypeWithGenericServices(
           IFakeMultipleService multipleService,
           IFakeService fakeService)
        {
        }

        public TypeWithGenericServices(
            IFakeService fakeService,
            IFactoryService factoryService,
            IFakeOpenGenericService<IFakeService> logger)
        {
        }
    }
}
