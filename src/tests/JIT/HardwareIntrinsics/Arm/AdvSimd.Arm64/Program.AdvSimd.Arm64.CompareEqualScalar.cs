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
                ["CompareEqualScalar.Vector64.Double"] = CompareEqualScalar_Vector64_Double,
                ["CompareEqualScalar.Vector64.Int64"] = CompareEqualScalar_Vector64_Int64,
                ["CompareEqualScalar.Vector64.Single"] = CompareEqualScalar_Vector64_Single,
                ["CompareEqualScalar.Vector64.UInt64"] = CompareEqualScalar_Vector64_UInt64,
            };
        }
    }
}
