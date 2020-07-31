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
                ["ExtractNarrowingLower.Vector64.Byte"] = ExtractNarrowingLower_Vector64_Byte,
                ["ExtractNarrowingLower.Vector64.Int16"] = ExtractNarrowingLower_Vector64_Int16,
                ["ExtractNarrowingLower.Vector64.Int32"] = ExtractNarrowingLower_Vector64_Int32,
                ["ExtractNarrowingLower.Vector64.SByte"] = ExtractNarrowingLower_Vector64_SByte,
                ["ExtractNarrowingLower.Vector64.UInt16"] = ExtractNarrowingLower_Vector64_UInt16,
                ["ExtractNarrowingLower.Vector64.UInt32"] = ExtractNarrowingLower_Vector64_UInt32,
            };
        }
    }
}
