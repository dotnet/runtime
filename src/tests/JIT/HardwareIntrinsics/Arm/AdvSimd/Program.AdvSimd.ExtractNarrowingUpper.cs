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
                ["ExtractNarrowingUpper.Vector128.Byte"] = ExtractNarrowingUpper_Vector128_Byte,
                ["ExtractNarrowingUpper.Vector128.Int16"] = ExtractNarrowingUpper_Vector128_Int16,
                ["ExtractNarrowingUpper.Vector128.Int32"] = ExtractNarrowingUpper_Vector128_Int32,
                ["ExtractNarrowingUpper.Vector128.SByte"] = ExtractNarrowingUpper_Vector128_SByte,
                ["ExtractNarrowingUpper.Vector128.UInt16"] = ExtractNarrowingUpper_Vector128_UInt16,
                ["ExtractNarrowingUpper.Vector128.UInt32"] = ExtractNarrowingUpper_Vector128_UInt32,
            };
        }
    }
}
