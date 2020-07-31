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
                ["ShiftLogicalRoundedSaturateScalar.Vector64.Byte"] = ShiftLogicalRoundedSaturateScalar_Vector64_Byte,
                ["ShiftLogicalRoundedSaturateScalar.Vector64.Int16"] = ShiftLogicalRoundedSaturateScalar_Vector64_Int16,
                ["ShiftLogicalRoundedSaturateScalar.Vector64.Int32"] = ShiftLogicalRoundedSaturateScalar_Vector64_Int32,
                ["ShiftLogicalRoundedSaturateScalar.Vector64.SByte"] = ShiftLogicalRoundedSaturateScalar_Vector64_SByte,
                ["ShiftLogicalRoundedSaturateScalar.Vector64.UInt16"] = ShiftLogicalRoundedSaturateScalar_Vector64_UInt16,
                ["ShiftLogicalRoundedSaturateScalar.Vector64.UInt32"] = ShiftLogicalRoundedSaturateScalar_Vector64_UInt32,
            };
        }
    }
}
