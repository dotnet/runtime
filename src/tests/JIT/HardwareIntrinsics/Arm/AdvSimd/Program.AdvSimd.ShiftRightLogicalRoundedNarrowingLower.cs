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
                ["ShiftRightLogicalRoundedNarrowingLower.Vector64.Byte.1"] = ShiftRightLogicalRoundedNarrowingLower_Vector64_Byte_1,
                ["ShiftRightLogicalRoundedNarrowingLower.Vector64.Int16.1"] = ShiftRightLogicalRoundedNarrowingLower_Vector64_Int16_1,
                ["ShiftRightLogicalRoundedNarrowingLower.Vector64.Int32.1"] = ShiftRightLogicalRoundedNarrowingLower_Vector64_Int32_1,
                ["ShiftRightLogicalRoundedNarrowingLower.Vector64.SByte.1"] = ShiftRightLogicalRoundedNarrowingLower_Vector64_SByte_1,
                ["ShiftRightLogicalRoundedNarrowingLower.Vector64.UInt16.1"] = ShiftRightLogicalRoundedNarrowingLower_Vector64_UInt16_1,
                ["ShiftRightLogicalRoundedNarrowingLower.Vector64.UInt32.1"] = ShiftRightLogicalRoundedNarrowingLower_Vector64_UInt32_1,
            };
        }
    }
}
