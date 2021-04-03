// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithAmbiguousCtorsAndAttributeFirst
    {
        [ActivatorUtilitiesConstructor]
        public ClassWithAmbiguousCtorsAndAttributeFirst(int dependency1, IFakeService dependency2, string dependency3)
        {
            CtorUsed = "int, IFakeService, string";
        }

        public ClassWithAmbiguousCtorsAndAttributeFirst(int dependency1, string dependency3)
        {
            CtorUsed = "int, string";
        }

        public string CtorUsed { get; set; }
    }
}
