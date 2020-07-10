// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["ComputeCrc32.UInt64"] = ComputeCrc32_UInt64,
                ["ComputeCrc32C.UInt64"] = ComputeCrc32C_UInt64,
            };
        }
    }
}
