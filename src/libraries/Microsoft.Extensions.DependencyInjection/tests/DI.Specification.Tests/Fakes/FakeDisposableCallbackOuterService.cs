// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            MultipleServices = multipleServices;
        }

        public IFakeService SingleService { get; }
        public IEnumerable<IFakeMultipleService> MultipleServices { get; }
    }
}