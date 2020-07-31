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
                ["BitwiseSelect.Vector64.Byte"] = BitwiseSelect_Vector64_Byte,
                ["BitwiseSelect.Vector64.Double"] = BitwiseSelect_Vector64_Double,
                ["BitwiseSelect.Vector64.Int16"] = BitwiseSelect_Vector64_Int16,
                ["BitwiseSelect.Vector64.Int32"] = BitwiseSelect_Vector64_Int32,
                ["BitwiseSelect.Vector64.Int64"] = BitwiseSelect_Vector64_Int64,
                ["BitwiseSelect.Vector64.SByte"] = BitwiseSelect_Vector64_SByte,
                ["BitwiseSelect.Vector64.Single"] = BitwiseSelect_Vector64_Single,
                ["BitwiseSelect.Vector64.UInt16"] = BitwiseSelect_Vector64_UInt16,
                ["BitwiseSelect.Vector64.UInt32"] = BitwiseSelect_Vector64_UInt32,
                ["BitwiseSelect.Vector64.UInt64"] = BitwiseSelect_Vector64_UInt64,
                ["BitwiseSelect.Vector128.Byte"] = BitwiseSelect_Vector128_Byte,
                ["BitwiseSelect.Vector128.Double"] = BitwiseSelect_Vector128_Double,
                ["BitwiseSelect.Vector128.Int16"] = BitwiseSelect_Vector128_Int16,
                ["BitwiseSelect.Vector128.Int32"] = BitwiseSelect_Vector128_Int32,
                ["BitwiseSelect.Vector128.Int64"] = BitwiseSelect_Vector128_Int64,
                ["BitwiseSelect.Vector128.SByte"] = BitwiseSelect_Vector128_SByte,
                ["BitwiseSelect.Vector128.Single"] = BitwiseSelect_Vector128_Single,
                ["BitwiseSelect.Vector128.UInt16"] = BitwiseSelect_Vector128_UInt16,
                ["BitwiseSelect.Vector128.UInt32"] = BitwiseSelect_Vector128_UInt32,
                ["BitwiseSelect.Vector128.UInt64"] = BitwiseSelect_Vector128_UInt64,
            };
        }
    }
}
