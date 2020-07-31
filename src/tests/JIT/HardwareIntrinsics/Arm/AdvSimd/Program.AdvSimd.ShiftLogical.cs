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
                ["ShiftLogical.Vector64.Byte"] = ShiftLogical_Vector64_Byte,
                ["ShiftLogical.Vector64.Int16"] = ShiftLogical_Vector64_Int16,
                ["ShiftLogical.Vector64.Int32"] = ShiftLogical_Vector64_Int32,
                ["ShiftLogical.Vector64.SByte"] = ShiftLogical_Vector64_SByte,
                ["ShiftLogical.Vector64.UInt16"] = ShiftLogical_Vector64_UInt16,
                ["ShiftLogical.Vector64.UInt32"] = ShiftLogical_Vector64_UInt32,
                ["ShiftLogical.Vector128.Byte"] = ShiftLogical_Vector128_Byte,
                ["ShiftLogical.Vector128.Int16"] = ShiftLogical_Vector128_Int16,
                ["ShiftLogical.Vector128.Int32"] = ShiftLogical_Vector128_Int32,
                ["ShiftLogical.Vector128.Int64"] = ShiftLogical_Vector128_Int64,
                ["ShiftLogical.Vector128.SByte"] = ShiftLogical_Vector128_SByte,
                ["ShiftLogical.Vector128.UInt16"] = ShiftLogical_Vector128_UInt16,
                ["ShiftLogical.Vector128.UInt32"] = ShiftLogical_Vector128_UInt32,
                ["ShiftLogical.Vector128.UInt64"] = ShiftLogical_Vector128_UInt64,
            };
        }
    }
}
