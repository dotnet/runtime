// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithMultipleAmbiguousCtorsAndAttribute
    {
        [ActivatorUtilitiesConstructor]
        public ClassWithMultipleAmbiguousCtorsAndAttribute(IFakeService service, string data)
        {
            CtorUsed = "IFakeService, string";
        }

        public ClassWithMultipleAmbiguousCtorsAndAttribute(int data1, string data2)
        {
            CtorUsed = "int, string";
        }

        public string CtorUsed { get; set; }
    }
}
