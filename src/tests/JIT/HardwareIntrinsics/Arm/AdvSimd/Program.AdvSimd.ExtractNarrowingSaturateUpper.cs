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
                ["ExtractNarrowingSaturateUpper.Vector128.Byte"] = ExtractNarrowingSaturateUpper_Vector128_Byte,
                ["ExtractNarrowingSaturateUpper.Vector128.Int16"] = ExtractNarrowingSaturateUpper_Vector128_Int16,
                ["ExtractNarrowingSaturateUpper.Vector128.Int32"] = ExtractNarrowingSaturateUpper_Vector128_Int32,
                ["ExtractNarrowingSaturateUpper.Vector128.SByte"] = ExtractNarrowingSaturateUpper_Vector128_SByte,
                ["ExtractNarrowingSaturateUpper.Vector128.UInt16"] = ExtractNarrowingSaturateUpper_Vector128_UInt16,
                ["ExtractNarrowingSaturateUpper.Vector128.UInt32"] = ExtractNarrowingSaturateUpper_Vector128_UInt32,
            };
        }
    }
}
