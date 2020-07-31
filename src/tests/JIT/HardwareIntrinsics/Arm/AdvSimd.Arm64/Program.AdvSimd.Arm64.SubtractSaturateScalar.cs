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
                ["SubtractSaturateScalar.Vector64.Byte"] = SubtractSaturateScalar_Vector64_Byte,
                ["SubtractSaturateScalar.Vector64.Int16"] = SubtractSaturateScalar_Vector64_Int16,
                ["SubtractSaturateScalar.Vector64.Int32"] = SubtractSaturateScalar_Vector64_Int32,
                ["SubtractSaturateScalar.Vector64.SByte"] = SubtractSaturateScalar_Vector64_SByte,
                ["SubtractSaturateScalar.Vector64.UInt16"] = SubtractSaturateScalar_Vector64_UInt16,
                ["SubtractSaturateScalar.Vector64.UInt32"] = SubtractSaturateScalar_Vector64_UInt32,
            };
        }
    }
}
