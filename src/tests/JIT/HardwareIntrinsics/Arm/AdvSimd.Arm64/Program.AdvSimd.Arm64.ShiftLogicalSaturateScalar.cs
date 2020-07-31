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
                ["ShiftLogicalSaturateScalar.Vector64.Byte"] = ShiftLogicalSaturateScalar_Vector64_Byte,
                ["ShiftLogicalSaturateScalar.Vector64.Int16"] = ShiftLogicalSaturateScalar_Vector64_Int16,
                ["ShiftLogicalSaturateScalar.Vector64.Int32"] = ShiftLogicalSaturateScalar_Vector64_Int32,
                ["ShiftLogicalSaturateScalar.Vector64.SByte"] = ShiftLogicalSaturateScalar_Vector64_SByte,
                ["ShiftLogicalSaturateScalar.Vector64.UInt16"] = ShiftLogicalSaturateScalar_Vector64_UInt16,
                ["ShiftLogicalSaturateScalar.Vector64.UInt32"] = ShiftLogicalSaturateScalar_Vector64_UInt32,
            };
        }
    }
}
