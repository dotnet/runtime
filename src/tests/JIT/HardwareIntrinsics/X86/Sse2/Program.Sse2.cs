// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

[assembly:Xunit.ActiveIssue("https://github.com/dotnet/runtime/issues/75767", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoLLVMAOT))]
[assembly:Xunit.ActiveIssue("https://github.com/dotnet/runtime/issues/102150", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoFULLAOT))]
namespace JIT.HardwareIntrinsics.X86._Sse2
{
    public static partial class Program
    {
        static Program()
        {

        }
    }
}
