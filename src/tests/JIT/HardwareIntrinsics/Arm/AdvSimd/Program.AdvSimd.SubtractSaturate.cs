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
                ["SubtractSaturate.Vector64.Byte"] = SubtractSaturate_Vector64_Byte,
                ["SubtractSaturate.Vector64.Int16"] = SubtractSaturate_Vector64_Int16,
                ["SubtractSaturate.Vector64.Int32"] = SubtractSaturate_Vector64_Int32,
                ["SubtractSaturate.Vector64.SByte"] = SubtractSaturate_Vector64_SByte,
                ["SubtractSaturate.Vector64.UInt16"] = SubtractSaturate_Vector64_UInt16,
                ["SubtractSaturate.Vector64.UInt32"] = SubtractSaturate_Vector64_UInt32,
                ["SubtractSaturate.Vector128.Byte"] = SubtractSaturate_Vector128_Byte,
                ["SubtractSaturate.Vector128.Int16"] = SubtractSaturate_Vector128_Int16,
                ["SubtractSaturate.Vector128.Int32"] = SubtractSaturate_Vector128_Int32,
                ["SubtractSaturate.Vector128.Int64"] = SubtractSaturate_Vector128_Int64,
                ["SubtractSaturate.Vector128.SByte"] = SubtractSaturate_Vector128_SByte,
                ["SubtractSaturate.Vector128.UInt16"] = SubtractSaturate_Vector128_UInt16,
                ["SubtractSaturate.Vector128.UInt32"] = SubtractSaturate_Vector128_UInt32,
                ["SubtractSaturate.Vector128.UInt64"] = SubtractSaturate_Vector128_UInt64,
            };
        }
    }
}
