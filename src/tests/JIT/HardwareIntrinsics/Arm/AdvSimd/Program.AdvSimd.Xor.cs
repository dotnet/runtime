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
                ["Xor.Vector64.Byte"] = Xor_Vector64_Byte,
                ["Xor.Vector64.Double"] = Xor_Vector64_Double,
                ["Xor.Vector64.Int16"] = Xor_Vector64_Int16,
                ["Xor.Vector64.Int32"] = Xor_Vector64_Int32,
                ["Xor.Vector64.Int64"] = Xor_Vector64_Int64,
                ["Xor.Vector64.SByte"] = Xor_Vector64_SByte,
                ["Xor.Vector64.Single"] = Xor_Vector64_Single,
                ["Xor.Vector64.UInt16"] = Xor_Vector64_UInt16,
                ["Xor.Vector64.UInt32"] = Xor_Vector64_UInt32,
                ["Xor.Vector64.UInt64"] = Xor_Vector64_UInt64,
                ["Xor.Vector128.Byte"] = Xor_Vector128_Byte,
                ["Xor.Vector128.Double"] = Xor_Vector128_Double,
                ["Xor.Vector128.Int16"] = Xor_Vector128_Int16,
                ["Xor.Vector128.Int32"] = Xor_Vector128_Int32,
                ["Xor.Vector128.Int64"] = Xor_Vector128_Int64,
                ["Xor.Vector128.SByte"] = Xor_Vector128_SByte,
                ["Xor.Vector128.Single"] = Xor_Vector128_Single,
                ["Xor.Vector128.UInt16"] = Xor_Vector128_UInt16,
                ["Xor.Vector128.UInt32"] = Xor_Vector128_UInt32,
                ["Xor.Vector128.UInt64"] = Xor_Vector128_UInt64,
            };
        }
    }
}
