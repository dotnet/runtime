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
            };
        }
    }
}
