// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class AnotherClassAcceptingData
    {
        public AnotherClassAcceptingData(IFakeService fakeService, string one, string two)
        {
            FakeService = fakeService;
            One = one;
            Two = two;
        }

        public IFakeService FakeService { get; }

        public string One { get; }

        public string Two { get; }
    }
}