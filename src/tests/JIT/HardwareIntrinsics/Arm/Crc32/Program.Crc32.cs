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
                ["ComputeCrc32.Byte"] = ComputeCrc32_Byte,
                ["ComputeCrc32.UInt16"] = ComputeCrc32_UInt16,
                ["ComputeCrc32.UInt32"] = ComputeCrc32_UInt32,
                ["ComputeCrc32C.Byte"] = ComputeCrc32C_Byte,
                ["ComputeCrc32C.UInt16"] = ComputeCrc32C_UInt16,
                ["ComputeCrc32C.UInt32"] = ComputeCrc32C_UInt32,
            };
        }
    }
}
