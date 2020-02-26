// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class FakeOuterService : IFakeOuterService
    {
        public FakeOuterService(
            IFakeService singleService,
            IEnumerable<IFakeMultipleService> multipleServices)
        {
            SingleService = singleService;
            MultipleServices = multipleServices;
        }

        public IFakeService SingleService { get; }

        public IEnumerable<IFakeMultipleService> MultipleServices { get; }
    }
}