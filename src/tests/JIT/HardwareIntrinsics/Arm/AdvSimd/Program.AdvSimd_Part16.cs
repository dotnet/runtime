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
            TestList = new Dictionary<string, Action>() {
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
                ["ZeroExtendWideningLower.Vector64.Byte"] = ZeroExtendWideningLower_Vector64_Byte,
                ["ZeroExtendWideningLower.Vector64.Int16"] = ZeroExtendWideningLower_Vector64_Int16,
                ["ZeroExtendWideningLower.Vector64.Int32"] = ZeroExtendWideningLower_Vector64_Int32,
                ["ZeroExtendWideningLower.Vector64.SByte"] = ZeroExtendWideningLower_Vector64_SByte,
                ["ZeroExtendWideningLower.Vector64.UInt16"] = ZeroExtendWideningLower_Vector64_UInt16,
                ["ZeroExtendWideningLower.Vector64.UInt32"] = ZeroExtendWideningLower_Vector64_UInt32,
                ["ZeroExtendWideningUpper.Vector128.Byte"] = ZeroExtendWideningUpper_Vector128_Byte,
                ["ZeroExtendWideningUpper.Vector128.Int16"] = ZeroExtendWideningUpper_Vector128_Int16,
                ["ZeroExtendWideningUpper.Vector128.Int32"] = ZeroExtendWideningUpper_Vector128_Int32,
                ["ZeroExtendWideningUpper.Vector128.SByte"] = ZeroExtendWideningUpper_Vector128_SByte,
                ["ZeroExtendWideningUpper.Vector128.UInt16"] = ZeroExtendWideningUpper_Vector128_UInt16,
                ["ZeroExtendWideningUpper.Vector128.UInt32"] = ZeroExtendWideningUpper_Vector128_UInt32,
            };
        }
    }
}
