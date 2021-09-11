// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.Diagnostics.Runtime;
using Xunit;

namespace System
{
    public sealed class IgnoreOnArchitectureTheoryAttribute : TheoryAttribute
    {
        public IgnoreOnArchitectureTheoryAttribute(bool x64)
        {
            if (x64 && !Environment.Is64BitProcess)
            {
                Skip = "Ignored in Non-64-bit process";
            } else if (!x64 && Environment.Is64BitProcess)
            {
                Skip = "Ignored in Non-32-bit process";
            }
        }
    }
}
