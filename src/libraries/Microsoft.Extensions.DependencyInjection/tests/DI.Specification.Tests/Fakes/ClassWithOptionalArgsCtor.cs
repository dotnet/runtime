// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
