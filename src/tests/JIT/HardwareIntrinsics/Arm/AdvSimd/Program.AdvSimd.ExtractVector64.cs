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
                ["ExtractVector64.Byte.1"] = ExtractVector64_Byte_1,
                ["ExtractVector64.Int16.1"] = ExtractVector64_Int16_1,
                ["ExtractVector64.Int32.1"] = ExtractVector64_Int32_1,
                ["ExtractVector64.SByte.1"] = ExtractVector64_SByte_1,
                ["ExtractVector64.Single.1"] = ExtractVector64_Single_1,
                ["ExtractVector64.UInt16.1"] = ExtractVector64_UInt16_1,
                ["ExtractVector64.UInt32.1"] = ExtractVector64_UInt32_1,
            };
        }
    }
}
