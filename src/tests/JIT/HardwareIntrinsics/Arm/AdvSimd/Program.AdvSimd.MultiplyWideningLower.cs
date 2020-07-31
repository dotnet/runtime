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
                ["MultiplyWideningLower.Vector64.Byte"] = MultiplyWideningLower_Vector64_Byte,
                ["MultiplyWideningLower.Vector64.Int16"] = MultiplyWideningLower_Vector64_Int16,
                ["MultiplyWideningLower.Vector64.Int32"] = MultiplyWideningLower_Vector64_Int32,
                ["MultiplyWideningLower.Vector64.SByte"] = MultiplyWideningLower_Vector64_SByte,
                ["MultiplyWideningLower.Vector64.UInt16"] = MultiplyWideningLower_Vector64_UInt16,
                ["MultiplyWideningLower.Vector64.UInt32"] = MultiplyWideningLower_Vector64_UInt32,
            };
        }
    }
}
