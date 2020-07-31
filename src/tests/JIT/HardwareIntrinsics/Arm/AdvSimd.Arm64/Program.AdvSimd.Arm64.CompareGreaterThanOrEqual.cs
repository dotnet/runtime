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
                ["CompareGreaterThanOrEqual.Vector128.Double"] = CompareGreaterThanOrEqual_Vector128_Double,
                ["CompareGreaterThanOrEqual.Vector128.Int64"] = CompareGreaterThanOrEqual_Vector128_Int64,
                ["CompareGreaterThanOrEqual.Vector128.UInt64"] = CompareGreaterThanOrEqual_Vector128_UInt64,
            };
        }
    }
}
