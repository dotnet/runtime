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
                ["ShiftRightAndInsert.Vector64.Byte"] = ShiftRightAndInsert_Vector64_Byte,
                ["ShiftRightAndInsert.Vector64.Int16"] = ShiftRightAndInsert_Vector64_Int16,
                ["ShiftRightAndInsert.Vector64.Int32"] = ShiftRightAndInsert_Vector64_Int32,
                ["ShiftRightAndInsert.Vector64.SByte"] = ShiftRightAndInsert_Vector64_SByte,
                ["ShiftRightAndInsert.Vector64.UInt16"] = ShiftRightAndInsert_Vector64_UInt16,
                ["ShiftRightAndInsert.Vector64.UInt32"] = ShiftRightAndInsert_Vector64_UInt32,
                ["ShiftRightAndInsert.Vector128.Byte"] = ShiftRightAndInsert_Vector128_Byte,
                ["ShiftRightAndInsert.Vector128.Int16"] = ShiftRightAndInsert_Vector128_Int16,
                ["ShiftRightAndInsert.Vector128.Int32"] = ShiftRightAndInsert_Vector128_Int32,
                ["ShiftRightAndInsert.Vector128.Int64"] = ShiftRightAndInsert_Vector128_Int64,
                ["ShiftRightAndInsert.Vector128.SByte"] = ShiftRightAndInsert_Vector128_SByte,
                ["ShiftRightAndInsert.Vector128.UInt16"] = ShiftRightAndInsert_Vector128_UInt16,
                ["ShiftRightAndInsert.Vector128.UInt32"] = ShiftRightAndInsert_Vector128_UInt32,
                ["ShiftRightAndInsert.Vector128.UInt64"] = ShiftRightAndInsert_Vector128_UInt64,
            };
        }
    }
}
