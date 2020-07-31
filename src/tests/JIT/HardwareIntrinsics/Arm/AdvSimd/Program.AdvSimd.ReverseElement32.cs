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
            TestList = new Dictionary<string, Action>()
            {
                ["ReverseElement32.Vector64.Int64"] = ReverseElement32_Vector64_Int64,
                ["ReverseElement32.Vector64.UInt64"] = ReverseElement32_Vector64_UInt64,
                ["ReverseElement32.Vector128.Int64"] = ReverseElement32_Vector128_Int64,
                ["ReverseElement32.Vector128.UInt64"] = ReverseElement32_Vector128_UInt64,
            };
        }
    }
}
