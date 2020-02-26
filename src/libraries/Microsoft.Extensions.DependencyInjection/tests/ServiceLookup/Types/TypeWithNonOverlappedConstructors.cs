// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
