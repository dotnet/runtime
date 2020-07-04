// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
