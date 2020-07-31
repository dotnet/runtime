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
                ["CompareGreaterThanOrEqualScalar.Vector64.Double"] = CompareGreaterThanOrEqualScalar_Vector64_Double,
                ["CompareGreaterThanOrEqualScalar.Vector64.Int64"] = CompareGreaterThanOrEqualScalar_Vector64_Int64,
                ["CompareGreaterThanOrEqualScalar.Vector64.Single"] = CompareGreaterThanOrEqualScalar_Vector64_Single,
                ["CompareGreaterThanOrEqualScalar.Vector64.UInt64"] = CompareGreaterThanOrEqualScalar_Vector64_UInt64,
            };
        }
    }
}
