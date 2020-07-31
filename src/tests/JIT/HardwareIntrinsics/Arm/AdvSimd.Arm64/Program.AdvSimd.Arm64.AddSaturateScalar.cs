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
                ["AddSaturateScalar.Vector64.Byte.Vector64.Byte"] = AddSaturateScalar_Vector64_Byte_Vector64_Byte,
                ["AddSaturateScalar.Vector64.Byte.Vector64.SByte"] = AddSaturateScalar_Vector64_Byte_Vector64_SByte,
                ["AddSaturateScalar.Vector64.Int16.Vector64.Int16"] = AddSaturateScalar_Vector64_Int16_Vector64_Int16,
                ["AddSaturateScalar.Vector64.Int16.Vector64.UInt16"] = AddSaturateScalar_Vector64_Int16_Vector64_UInt16,
                ["AddSaturateScalar.Vector64.Int32.Vector64.Int32"] = AddSaturateScalar_Vector64_Int32_Vector64_Int32,
                ["AddSaturateScalar.Vector64.Int32.Vector64.UInt32"] = AddSaturateScalar_Vector64_Int32_Vector64_UInt32,
                ["AddSaturateScalar.Vector64.Int64.Vector64.UInt64"] = AddSaturateScalar_Vector64_Int64_Vector64_UInt64,
                ["AddSaturateScalar.Vector64.SByte.Vector64.Byte"] = AddSaturateScalar_Vector64_SByte_Vector64_Byte,
                ["AddSaturateScalar.Vector64.SByte.Vector64.SByte"] = AddSaturateScalar_Vector64_SByte_Vector64_SByte,
                ["AddSaturateScalar.Vector64.UInt16.Vector64.Int16"] = AddSaturateScalar_Vector64_UInt16_Vector64_Int16,
                ["AddSaturateScalar.Vector64.UInt16.Vector64.UInt16"] = AddSaturateScalar_Vector64_UInt16_Vector64_UInt16,
                ["AddSaturateScalar.Vector64.UInt32.Vector64.Int32"] = AddSaturateScalar_Vector64_UInt32_Vector64_Int32,
                ["AddSaturateScalar.Vector64.UInt32.Vector64.UInt32"] = AddSaturateScalar_Vector64_UInt32_Vector64_UInt32,
                ["AddSaturateScalar.Vector64.UInt64.Vector64.Int64"] = AddSaturateScalar_Vector64_UInt64_Vector64_Int64,
            };
        }
    }
}
