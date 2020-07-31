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
                ["ExtractVector128.Byte.1"] = ExtractVector128_Byte_1,
                ["ExtractVector128.Double.1"] = ExtractVector128_Double_1,
                ["ExtractVector128.Int16.1"] = ExtractVector128_Int16_1,
                ["ExtractVector128.Int32.1"] = ExtractVector128_Int32_1,
                ["ExtractVector128.Int64.1"] = ExtractVector128_Int64_1,
                ["ExtractVector128.SByte.1"] = ExtractVector128_SByte_1,
                ["ExtractVector128.Single.1"] = ExtractVector128_Single_1,
                ["ExtractVector128.UInt16.1"] = ExtractVector128_UInt16_1,
                ["ExtractVector128.UInt32.1"] = ExtractVector128_UInt32_1,
                ["ExtractVector128.UInt64.1"] = ExtractVector128_UInt64_1,
            };
        }
    }
}
