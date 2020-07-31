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
                ["CompareLessThanOrEqualScalar.Vector64.Double"] = CompareLessThanOrEqualScalar_Vector64_Double,
                ["CompareLessThanOrEqualScalar.Vector64.Int64"] = CompareLessThanOrEqualScalar_Vector64_Int64,
                ["CompareLessThanOrEqualScalar.Vector64.Single"] = CompareLessThanOrEqualScalar_Vector64_Single,
                ["CompareLessThanOrEqualScalar.Vector64.UInt64"] = CompareLessThanOrEqualScalar_Vector64_UInt64,
            };
        }
    }
}
