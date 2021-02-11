// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public class ClassWithOptionalArgsCtor
    {
        public ClassWithOptionalArgsCtor(string whatever = "BLARGH")
        {
            Whatever = whatever;
        }

        public string Whatever { get; set; }
    }
}
