// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class FakeDisposableCallbackOuterService : FakeDisposableCallbackService, IFakeOuterService
    {
        public FakeDisposableCallbackOuterService(
            IFakeService singleService,
            IEnumerable<IFakeMultipleService> multipleServices,
            FakeDisposeCallback callback) : base(callback)
        {
            SingleService = singleService;
            MultipleServices = multipleServices.ToArray();
        }

        public IFakeService SingleService { get; }
        public IEnumerable<IFakeMultipleService> MultipleServices { get; }
    }
}
