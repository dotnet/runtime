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
                ["ExtractNarrowingSaturateUnsignedLower.Vector64.Byte"] = ExtractNarrowingSaturateUnsignedLower_Vector64_Byte,
                ["ExtractNarrowingSaturateUnsignedLower.Vector64.UInt16"] = ExtractNarrowingSaturateUnsignedLower_Vector64_UInt16,
                ["ExtractNarrowingSaturateUnsignedLower.Vector64.UInt32"] = ExtractNarrowingSaturateUnsignedLower_Vector64_UInt32,
            };
        }
    }
}
