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
                ["DuplicateToVector64.Byte"] = DuplicateToVector64_Byte,
                ["DuplicateToVector64.Byte.31"] = DuplicateToVector64_Byte_31,
                ["DuplicateToVector64.Int16"] = DuplicateToVector64_Int16,
                ["DuplicateToVector64.Int16.31"] = DuplicateToVector64_Int16_31,
                ["DuplicateToVector64.Int32"] = DuplicateToVector64_Int32,
                ["DuplicateToVector64.Int32.31"] = DuplicateToVector64_Int32_31,
                ["DuplicateToVector64.SByte"] = DuplicateToVector64_SByte,
                ["DuplicateToVector64.SByte.31"] = DuplicateToVector64_SByte_31,
                ["DuplicateToVector64.Single"] = DuplicateToVector64_Single,
                ["DuplicateToVector64.Single.31"] = DuplicateToVector64_Single_31,
                ["DuplicateToVector64.UInt16"] = DuplicateToVector64_UInt16,
                ["DuplicateToVector64.UInt16.31"] = DuplicateToVector64_UInt16_31,
                ["DuplicateToVector64.UInt32"] = DuplicateToVector64_UInt32,
                ["DuplicateToVector64.UInt32.31"] = DuplicateToVector64_UInt32_31,
            };
        }
    }
}
