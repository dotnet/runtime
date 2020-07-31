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
                ["MultiplySubtractByScalar.Vector64.Int16"] = MultiplySubtractByScalar_Vector64_Int16,
                ["MultiplySubtractByScalar.Vector64.Int32"] = MultiplySubtractByScalar_Vector64_Int32,
                ["MultiplySubtractByScalar.Vector64.UInt16"] = MultiplySubtractByScalar_Vector64_UInt16,
                ["MultiplySubtractByScalar.Vector64.UInt32"] = MultiplySubtractByScalar_Vector64_UInt32,
                ["MultiplySubtractByScalar.Vector128.Int16"] = MultiplySubtractByScalar_Vector128_Int16,
                ["MultiplySubtractByScalar.Vector128.Int32"] = MultiplySubtractByScalar_Vector128_Int32,
                ["MultiplySubtractByScalar.Vector128.UInt16"] = MultiplySubtractByScalar_Vector128_UInt16,
                ["MultiplySubtractByScalar.Vector128.UInt32"] = MultiplySubtractByScalar_Vector128_UInt32,
            };
        }
    }
}
