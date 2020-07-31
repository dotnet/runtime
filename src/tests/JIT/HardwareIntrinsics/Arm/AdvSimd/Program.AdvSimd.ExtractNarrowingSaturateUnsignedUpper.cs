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
                ["ExtractNarrowingSaturateUnsignedUpper.Vector128.Byte"] = ExtractNarrowingSaturateUnsignedUpper_Vector128_Byte,
                ["ExtractNarrowingSaturateUnsignedUpper.Vector128.UInt16"] = ExtractNarrowingSaturateUnsignedUpper_Vector128_UInt16,
                ["ExtractNarrowingSaturateUnsignedUpper.Vector128.UInt32"] = ExtractNarrowingSaturateUnsignedUpper_Vector128_UInt32,
            };
        }
    }
}
