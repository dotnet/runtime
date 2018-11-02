// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
