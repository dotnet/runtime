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
                ["CompareLessThanScalar.Vector64.Double"] = CompareLessThanScalar_Vector64_Double,
                ["CompareLessThanScalar.Vector64.Int64"] = CompareLessThanScalar_Vector64_Int64,
                ["CompareLessThanScalar.Vector64.Single"] = CompareLessThanScalar_Vector64_Single,
                ["CompareLessThanScalar.Vector64.UInt64"] = CompareLessThanScalar_Vector64_UInt64,
            };
        }
    }
}
