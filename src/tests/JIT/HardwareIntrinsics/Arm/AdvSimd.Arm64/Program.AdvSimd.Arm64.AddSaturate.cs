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
                ["AddSaturate.Vector64.Byte.Vector64.SByte"] = AddSaturate_Vector64_Byte_Vector64_SByte,
                ["AddSaturate.Vector64.Int16.Vector64.UInt16"] = AddSaturate_Vector64_Int16_Vector64_UInt16,
                ["AddSaturate.Vector64.Int32.Vector64.UInt32"] = AddSaturate_Vector64_Int32_Vector64_UInt32,
                ["AddSaturate.Vector64.SByte.Vector64.Byte"] = AddSaturate_Vector64_SByte_Vector64_Byte,
                ["AddSaturate.Vector64.UInt16.Vector64.Int16"] = AddSaturate_Vector64_UInt16_Vector64_Int16,
                ["AddSaturate.Vector64.UInt32.Vector64.Int32"] = AddSaturate_Vector64_UInt32_Vector64_Int32,
                ["AddSaturate.Vector128.Byte.Vector128.SByte"] = AddSaturate_Vector128_Byte_Vector128_SByte,
                ["AddSaturate.Vector128.Int16.Vector128.UInt16"] = AddSaturate_Vector128_Int16_Vector128_UInt16,
                ["AddSaturate.Vector128.Int32.Vector128.UInt32"] = AddSaturate_Vector128_Int32_Vector128_UInt32,
                ["AddSaturate.Vector128.Int64.Vector128.UInt64"] = AddSaturate_Vector128_Int64_Vector128_UInt64,
                ["AddSaturate.Vector128.SByte.Vector128.Byte"] = AddSaturate_Vector128_SByte_Vector128_Byte,
                ["AddSaturate.Vector128.UInt16.Vector128.Int16"] = AddSaturate_Vector128_UInt16_Vector128_Int16,
                ["AddSaturate.Vector128.UInt32.Vector128.Int32"] = AddSaturate_Vector128_UInt32_Vector128_Int32,
                ["AddSaturate.Vector128.UInt64.Vector128.Int64"] = AddSaturate_Vector128_UInt64_Vector128_Int64,
            };
        }
    }
}
