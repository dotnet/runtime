// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using TestLibrary;
using Xunit;

namespace JIT.HardwareIntrinsics.Arm._Sve2
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/127955", typeof(CoreClrConfigurationDetection), nameof(CoreClrConfigurationDetection.IsAnyJitStress))]
    public static partial class Program
    {
        static Program()
        {
            JIT.HardwareIntrinsics.Arm.Program.PrintSupportedIsa();
        }
    }
}
