// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class TypeWithUnresolvableEnumerableConstructors
    {
        public TypeWithUnresolvableEnumerableConstructors(IFakeService fakeService)
        {
        }

        public TypeWithUnresolvableEnumerableConstructors(IEnumerable<IFakeService> fakeService)
        {
        }

        public TypeWithUnresolvableEnumerableConstructors(IFactoryService factoryService)
        {
        }
    }
}
