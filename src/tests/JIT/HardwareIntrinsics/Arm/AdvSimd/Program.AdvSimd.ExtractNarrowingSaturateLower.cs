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
                ["ExtractNarrowingSaturateLower.Vector64.Byte"] = ExtractNarrowingSaturateLower_Vector64_Byte,
                ["ExtractNarrowingSaturateLower.Vector64.Int16"] = ExtractNarrowingSaturateLower_Vector64_Int16,
                ["ExtractNarrowingSaturateLower.Vector64.Int32"] = ExtractNarrowingSaturateLower_Vector64_Int32,
                ["ExtractNarrowingSaturateLower.Vector64.SByte"] = ExtractNarrowingSaturateLower_Vector64_SByte,
                ["ExtractNarrowingSaturateLower.Vector64.UInt16"] = ExtractNarrowingSaturateLower_Vector64_UInt16,
                ["ExtractNarrowingSaturateLower.Vector64.UInt32"] = ExtractNarrowingSaturateLower_Vector64_UInt32,
            };
        }
    }
}
