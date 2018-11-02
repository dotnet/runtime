// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class AnotherClass
    {
        public AnotherClass(IFakeService fakeService)
        {
            FakeService = fakeService;
        }

        public IFakeService FakeService { get; }
    }
}
