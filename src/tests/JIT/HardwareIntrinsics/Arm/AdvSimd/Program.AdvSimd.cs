// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias CoreLib;
global using AdvSimd = CoreLib::System.Runtime.Intrinsics.Arm.AdvSimd;
using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm._AdvSimd
{
    public static partial class Program
    {
        static Program()
        {
            JIT.HardwareIntrinsics.Arm.Program.PrintSupportedIsa();
        }
    }
}
