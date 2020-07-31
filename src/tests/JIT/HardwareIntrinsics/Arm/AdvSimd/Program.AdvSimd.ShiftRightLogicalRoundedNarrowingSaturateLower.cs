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
                ["ShiftRightLogicalRoundedNarrowingSaturateLower.Vector64.Byte.1"] = ShiftRightLogicalRoundedNarrowingSaturateLower_Vector64_Byte_1,
                ["ShiftRightLogicalRoundedNarrowingSaturateLower.Vector64.Int16.1"] = ShiftRightLogicalRoundedNarrowingSaturateLower_Vector64_Int16_1,
                ["ShiftRightLogicalRoundedNarrowingSaturateLower.Vector64.Int32.1"] = ShiftRightLogicalRoundedNarrowingSaturateLower_Vector64_Int32_1,
                ["ShiftRightLogicalRoundedNarrowingSaturateLower.Vector64.SByte.1"] = ShiftRightLogicalRoundedNarrowingSaturateLower_Vector64_SByte_1,
                ["ShiftRightLogicalRoundedNarrowingSaturateLower.Vector64.UInt16.1"] = ShiftRightLogicalRoundedNarrowingSaturateLower_Vector64_UInt16_1,
                ["ShiftRightLogicalRoundedNarrowingSaturateLower.Vector64.UInt32.1"] = ShiftRightLogicalRoundedNarrowingSaturateLower_Vector64_UInt32_1,
            };
        }
    }
}
