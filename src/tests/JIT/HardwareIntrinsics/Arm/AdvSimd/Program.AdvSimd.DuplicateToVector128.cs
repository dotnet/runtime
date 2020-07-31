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
                ["DuplicateToVector128.Byte"] = DuplicateToVector128_Byte,
                ["DuplicateToVector128.Byte.31"] = DuplicateToVector128_Byte_31,
                ["DuplicateToVector128.Int16"] = DuplicateToVector128_Int16,
                ["DuplicateToVector128.Int16.31"] = DuplicateToVector128_Int16_31,
                ["DuplicateToVector128.Int32"] = DuplicateToVector128_Int32,
                ["DuplicateToVector128.Int32.31"] = DuplicateToVector128_Int32_31,
                ["DuplicateToVector128.SByte"] = DuplicateToVector128_SByte,
                ["DuplicateToVector128.SByte.31"] = DuplicateToVector128_SByte_31,
                ["DuplicateToVector128.Single"] = DuplicateToVector128_Single,
                ["DuplicateToVector128.Single.31"] = DuplicateToVector128_Single_31,
                ["DuplicateToVector128.UInt16"] = DuplicateToVector128_UInt16,
                ["DuplicateToVector128.UInt16.31"] = DuplicateToVector128_UInt16_31,
                ["DuplicateToVector128.UInt32"] = DuplicateToVector128_UInt32,
                ["DuplicateToVector128.UInt32.31"] = DuplicateToVector128_UInt32_31,
            };
        }
    }
}
