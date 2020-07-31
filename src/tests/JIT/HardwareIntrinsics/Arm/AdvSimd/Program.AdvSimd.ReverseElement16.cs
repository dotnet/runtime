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
                ["ReverseElement16.Vector64.Int32"] = ReverseElement16_Vector64_Int32,
                ["ReverseElement16.Vector64.Int64"] = ReverseElement16_Vector64_Int64,
                ["ReverseElement16.Vector64.UInt32"] = ReverseElement16_Vector64_UInt32,
                ["ReverseElement16.Vector64.UInt64"] = ReverseElement16_Vector64_UInt64,
                ["ReverseElement16.Vector128.Int32"] = ReverseElement16_Vector128_Int32,
                ["ReverseElement16.Vector128.Int64"] = ReverseElement16_Vector128_Int64,
                ["ReverseElement16.Vector128.UInt32"] = ReverseElement16_Vector128_UInt32,
                ["ReverseElement16.Vector128.UInt64"] = ReverseElement16_Vector128_UInt64,
            };
        }
    }
}
