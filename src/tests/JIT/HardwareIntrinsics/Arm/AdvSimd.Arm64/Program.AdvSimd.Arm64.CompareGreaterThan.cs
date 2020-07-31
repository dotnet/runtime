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
                ["CompareGreaterThan.Vector128.Double"] = CompareGreaterThan_Vector128_Double,
                ["CompareGreaterThan.Vector128.Int64"] = CompareGreaterThan_Vector128_Int64,
                ["CompareGreaterThan.Vector128.UInt64"] = CompareGreaterThan_Vector128_UInt64,
            };
        }
    }
}
