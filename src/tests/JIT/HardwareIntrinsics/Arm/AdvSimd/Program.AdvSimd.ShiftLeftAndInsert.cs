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
                ["ShiftLeftAndInsert.Vector64.Byte"] = ShiftLeftAndInsert_Vector64_Byte,
                ["ShiftLeftAndInsert.Vector64.Int16"] = ShiftLeftAndInsert_Vector64_Int16,
                ["ShiftLeftAndInsert.Vector64.Int32"] = ShiftLeftAndInsert_Vector64_Int32,
                ["ShiftLeftAndInsert.Vector64.SByte"] = ShiftLeftAndInsert_Vector64_SByte,
                ["ShiftLeftAndInsert.Vector64.UInt16"] = ShiftLeftAndInsert_Vector64_UInt16,
                ["ShiftLeftAndInsert.Vector64.UInt32"] = ShiftLeftAndInsert_Vector64_UInt32,
                ["ShiftLeftAndInsert.Vector128.Byte"] = ShiftLeftAndInsert_Vector128_Byte,
                ["ShiftLeftAndInsert.Vector128.Int16"] = ShiftLeftAndInsert_Vector128_Int16,
                ["ShiftLeftAndInsert.Vector128.Int32"] = ShiftLeftAndInsert_Vector128_Int32,
                ["ShiftLeftAndInsert.Vector128.Int64"] = ShiftLeftAndInsert_Vector128_Int64,
                ["ShiftLeftAndInsert.Vector128.SByte"] = ShiftLeftAndInsert_Vector128_SByte,
                ["ShiftLeftAndInsert.Vector128.UInt16"] = ShiftLeftAndInsert_Vector128_UInt16,
                ["ShiftLeftAndInsert.Vector128.UInt32"] = ShiftLeftAndInsert_Vector128_UInt32,
                ["ShiftLeftAndInsert.Vector128.UInt64"] = ShiftLeftAndInsert_Vector128_UInt64,
            };
        }
    }
}
