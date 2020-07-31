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
                ["AddWideningLower.Vector64.Byte"] = AddWideningLower_Vector64_Byte,
                ["AddWideningLower.Vector64.Int16"] = AddWideningLower_Vector64_Int16,
                ["AddWideningLower.Vector64.Int32"] = AddWideningLower_Vector64_Int32,
                ["AddWideningLower.Vector64.SByte"] = AddWideningLower_Vector64_SByte,
                ["AddWideningLower.Vector64.UInt16"] = AddWideningLower_Vector64_UInt16,
                ["AddWideningLower.Vector64.UInt32"] = AddWideningLower_Vector64_UInt32,
                ["AddWideningLower.Vector128.Int16"] = AddWideningLower_Vector128_Int16,
                ["AddWideningLower.Vector128.Int32"] = AddWideningLower_Vector128_Int32,
                ["AddWideningLower.Vector128.Int64"] = AddWideningLower_Vector128_Int64,
                ["AddWideningLower.Vector128.UInt16"] = AddWideningLower_Vector128_UInt16,
                ["AddWideningLower.Vector128.UInt32"] = AddWideningLower_Vector128_UInt32,
                ["AddWideningLower.Vector128.UInt64"] = AddWideningLower_Vector128_UInt64,
            };
        }
    }
}
