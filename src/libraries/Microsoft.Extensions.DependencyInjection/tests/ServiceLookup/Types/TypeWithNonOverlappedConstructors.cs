// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class TypeWithNonOverlappedConstructors
    {
        public TypeWithNonOverlappedConstructors(
            IFakeOuterService outerService)
        {
        }

        public TypeWithNonOverlappedConstructors(
            IFakeScopedService scopedService,
            IFakeService fakeService)
        {
        }

        public TypeWithNonOverlappedConstructors(
            IFakeScopedService scopedService,
            IFakeService fakeService,
            IFakeMultipleService multipleService)
        {
        }
    }
}
