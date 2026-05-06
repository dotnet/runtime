// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
