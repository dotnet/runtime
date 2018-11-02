// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithAmbiguousCtorsAndAttribute
    {
        public ClassWithAmbiguousCtorsAndAttribute(string data)
        {
            CtorUsed = "string";
        }

        [ActivatorUtilitiesConstructor]
        public ClassWithAmbiguousCtorsAndAttribute(IFakeService service, string data)
        {
            CtorUsed = "IFakeService, string";
        }

        public ClassWithAmbiguousCtorsAndAttribute(IFakeService service, IFakeOuterService service2, string data)
        {
            CtorUsed = "IFakeService, IFakeService, string";
        }

        public string CtorUsed { get; set; }
    }
}