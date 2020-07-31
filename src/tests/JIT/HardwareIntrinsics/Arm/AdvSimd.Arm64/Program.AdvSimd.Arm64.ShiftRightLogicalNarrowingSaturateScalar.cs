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
                ["ShiftRightLogicalNarrowingSaturateScalar.Vector64.Byte.5"] = ShiftRightLogicalNarrowingSaturateScalar_Vector64_Byte_5,
                ["ShiftRightLogicalNarrowingSaturateScalar.Vector64.Int16.7"] = ShiftRightLogicalNarrowingSaturateScalar_Vector64_Int16_7,
                ["ShiftRightLogicalNarrowingSaturateScalar.Vector64.Int32.11"] = ShiftRightLogicalNarrowingSaturateScalar_Vector64_Int32_11,
                ["ShiftRightLogicalNarrowingSaturateScalar.Vector64.SByte.3"] = ShiftRightLogicalNarrowingSaturateScalar_Vector64_SByte_3,
                ["ShiftRightLogicalNarrowingSaturateScalar.Vector64.UInt16.5"] = ShiftRightLogicalNarrowingSaturateScalar_Vector64_UInt16_5,
                ["ShiftRightLogicalNarrowingSaturateScalar.Vector64.UInt32.7"] = ShiftRightLogicalNarrowingSaturateScalar_Vector64_UInt32_7,
            };
        }
    }
}
