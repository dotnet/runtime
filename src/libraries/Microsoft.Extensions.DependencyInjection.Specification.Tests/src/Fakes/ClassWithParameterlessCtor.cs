// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithParameterlessCtor
    {
        public ClassWithParameterlessCtor(IFakeService dependency)
        {
            CtorUsed = "IFakeService";
        }

        public ClassWithParameterlessCtor()
        {
            CtorUsed = "Parameterless";
        }

        public string CtorUsed { get; set; }
    }
}
