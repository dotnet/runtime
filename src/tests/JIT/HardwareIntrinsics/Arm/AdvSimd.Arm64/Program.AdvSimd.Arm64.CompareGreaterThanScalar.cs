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
                ["CompareGreaterThanScalar.Vector64.Double"] = CompareGreaterThanScalar_Vector64_Double,
                ["CompareGreaterThanScalar.Vector64.Int64"] = CompareGreaterThanScalar_Vector64_Int64,
                ["CompareGreaterThanScalar.Vector64.Single"] = CompareGreaterThanScalar_Vector64_Single,
                ["CompareGreaterThanScalar.Vector64.UInt64"] = CompareGreaterThanScalar_Vector64_UInt64,
            };
        }
    }
}
