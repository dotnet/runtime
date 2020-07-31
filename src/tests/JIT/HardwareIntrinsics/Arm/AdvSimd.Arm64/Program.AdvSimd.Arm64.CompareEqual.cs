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
                ["CompareEqual.Vector128.Double"] = CompareEqual_Vector128_Double,
                ["CompareEqual.Vector128.Int64"] = CompareEqual_Vector128_Int64,
                ["CompareEqual.Vector128.UInt64"] = CompareEqual_Vector128_UInt64,
            };
        }
    }
}
