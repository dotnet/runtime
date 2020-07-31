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
                ["ShiftLeftLogicalSaturateUnsignedScalar.Vector64.Int16.5"] = ShiftLeftLogicalSaturateUnsignedScalar_Vector64_Int16_5,
                ["ShiftLeftLogicalSaturateUnsignedScalar.Vector64.Int32.7"] = ShiftLeftLogicalSaturateUnsignedScalar_Vector64_Int32_7,
                ["ShiftLeftLogicalSaturateUnsignedScalar.Vector64.SByte.3"] = ShiftLeftLogicalSaturateUnsignedScalar_Vector64_SByte_3,
            };
        }
    }
}
