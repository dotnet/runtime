// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithMultipleMarkedCtors
    {
        [ActivatorUtilitiesConstructor]
        public ClassWithMultipleMarkedCtors(string data)
        {
        }

        [ActivatorUtilitiesConstructor]
        public ClassWithMultipleMarkedCtors(IFakeService service, string data)
        {
        }
    }
}