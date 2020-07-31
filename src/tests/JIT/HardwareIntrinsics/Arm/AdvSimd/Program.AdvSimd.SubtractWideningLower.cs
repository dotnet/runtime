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
            };
        }
    }
}
