// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["SubtractSaturateScalar.Vector64.Int64"] = SubtractSaturateScalar_Vector64_Int64,
                ["SubtractSaturateScalar.Vector64.UInt64"] = SubtractSaturateScalar_Vector64_UInt64,
                ["SubtractScalar.Vector64.Double"] = SubtractScalar_Vector64_Double,
                ["SubtractScalar.Vector64.Int64"] = SubtractScalar_Vector64_Int64,
                ["SubtractScalar.Vector64.Single"] = SubtractScalar_Vector64_Single,
                ["SubtractScalar.Vector64.UInt64"] = SubtractScalar_Vector64_UInt64,
                ["SubtractWideningLower.Vector64.Byte"] = SubtractWideningLower_Vector64_Byte,
                ["SubtractWideningLower.Vector64.Int16"] = SubtractWideningLower_Vector64_Int16,
                ["SubtractWideningLower.Vector64.Int32"] = SubtractWideningLower_Vector64_Int32,
                ["SubtractWideningLower.Vector64.SByte"] = SubtractWideningLower_Vector64_SByte,
                ["SubtractWideningLower.Vector64.UInt16"] = SubtractWideningLower_Vector64_UInt16,
                ["SubtractWideningLower.Vector64.UInt32"] = SubtractWideningLower_Vector64_UInt32,
                ["SubtractWideningLower.Vector128.Int16"] = SubtractWideningLower_Vector128_Int16,
                ["SubtractWideningLower.Vector128.Int32"] = SubtractWideningLower_Vector128_Int32,
                ["SubtractWideningLower.Vector128.Int64"] = SubtractWideningLower_Vector128_Int64,
                ["SubtractWideningLower.Vector128.UInt16"] = SubtractWideningLower_Vector128_UInt16,
                ["SubtractWideningLower.Vector128.UInt32"] = SubtractWideningLower_Vector128_UInt32,
                ["SubtractWideningLower.Vector128.UInt64"] = SubtractWideningLower_Vector128_UInt64,
                ["SubtractWideningUpper.Vector128.Byte.Vector128.Byte"] = SubtractWideningUpper_Vector128_Byte_Vector128_Byte,
                ["SubtractWideningUpper.Vector128.Int16.Vector128.Int16"] = SubtractWideningUpper_Vector128_Int16_Vector128_Int16,
                ["SubtractWideningUpper.Vector128.Int16.Vector128.SByte"] = SubtractWideningUpper_Vector128_Int16_Vector128_SByte,
                ["SubtractWideningUpper.Vector128.Int32.Vector128.Int16"] = SubtractWideningUpper_Vector128_Int32_Vector128_Int16,
                ["SubtractWideningUpper.Vector128.Int32.Vector128.Int32"] = SubtractWideningUpper_Vector128_Int32_Vector128_Int32,
                ["SubtractWideningUpper.Vector128.Int64.Vector128.Int32"] = SubtractWideningUpper_Vector128_Int64_Vector128_Int32,
                ["SubtractWideningUpper.Vector128.SByte.Vector128.SByte"] = SubtractWideningUpper_Vector128_SByte_Vector128_SByte,
                ["SubtractWideningUpper.Vector128.UInt16.Vector128.Byte"] = SubtractWideningUpper_Vector128_UInt16_Vector128_Byte,
                ["SubtractWideningUpper.Vector128.UInt16.Vector128.UInt16"] = SubtractWideningUpper_Vector128_UInt16_Vector128_UInt16,
                ["SubtractWideningUpper.Vector128.UInt32.Vector128.UInt16"] = SubtractWideningUpper_Vector128_UInt32_Vector128_UInt16,
                ["SubtractWideningUpper.Vector128.UInt32.Vector128.UInt32"] = SubtractWideningUpper_Vector128_UInt32_Vector128_UInt32,
                ["SubtractWideningUpper.Vector128.UInt64.Vector128.UInt32"] = SubtractWideningUpper_Vector128_UInt64_Vector128_UInt32,
                ["VectorTableLookup.Vector64.Byte"] = VectorTableLookup_Vector64_Byte,
                ["VectorTableLookup.Vector64.SByte"] = VectorTableLookup_Vector64_SByte,
                ["VectorTableLookupExtension.Vector64.Byte"] = VectorTableLookupExtension_Vector64_Byte,
                ["VectorTableLookupExtension.Vector64.SByte"] = VectorTableLookupExtension_Vector64_SByte,
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
