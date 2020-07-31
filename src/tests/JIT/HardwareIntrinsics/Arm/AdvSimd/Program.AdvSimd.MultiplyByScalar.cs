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
                ["MultiplyByScalar.Vector64.Int16"] = MultiplyByScalar_Vector64_Int16,
                ["MultiplyByScalar.Vector64.Int32"] = MultiplyByScalar_Vector64_Int32,
                ["MultiplyByScalar.Vector64.Single"] = MultiplyByScalar_Vector64_Single,
                ["MultiplyByScalar.Vector64.UInt16"] = MultiplyByScalar_Vector64_UInt16,
                ["MultiplyByScalar.Vector64.UInt32"] = MultiplyByScalar_Vector64_UInt32,
                ["MultiplyByScalar.Vector128.Int16"] = MultiplyByScalar_Vector128_Int16,
                ["MultiplyByScalar.Vector128.Int32"] = MultiplyByScalar_Vector128_Int32,
                ["MultiplyByScalar.Vector128.Single"] = MultiplyByScalar_Vector128_Single,
                ["MultiplyByScalar.Vector128.UInt16"] = MultiplyByScalar_Vector128_UInt16,
                ["MultiplyByScalar.Vector128.UInt32"] = MultiplyByScalar_Vector128_UInt32,
            };
        }
    }
}
