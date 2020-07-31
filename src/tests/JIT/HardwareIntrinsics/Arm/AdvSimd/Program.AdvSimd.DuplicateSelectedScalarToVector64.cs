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
                ["DuplicateSelectedScalarToVector64.Vector64.Byte.1"] = DuplicateSelectedScalarToVector64_Vector64_Byte_1,
                ["DuplicateSelectedScalarToVector64.Vector64.Int16.1"] = DuplicateSelectedScalarToVector64_Vector64_Int16_1,
                ["DuplicateSelectedScalarToVector64.Vector64.Int32.1"] = DuplicateSelectedScalarToVector64_Vector64_Int32_1,
                ["DuplicateSelectedScalarToVector64.Vector64.SByte.1"] = DuplicateSelectedScalarToVector64_Vector64_SByte_1,
                ["DuplicateSelectedScalarToVector64.Vector64.Single.1"] = DuplicateSelectedScalarToVector64_Vector64_Single_1,
                ["DuplicateSelectedScalarToVector64.Vector64.UInt16.1"] = DuplicateSelectedScalarToVector64_Vector64_UInt16_1,
                ["DuplicateSelectedScalarToVector64.Vector64.UInt32.1"] = DuplicateSelectedScalarToVector64_Vector64_UInt32_1,
                ["DuplicateSelectedScalarToVector64.Vector128.Byte.8"] = DuplicateSelectedScalarToVector64_Vector128_Byte_8,
                ["DuplicateSelectedScalarToVector64.Vector128.Int16.4"] = DuplicateSelectedScalarToVector64_Vector128_Int16_4,
                ["DuplicateSelectedScalarToVector64.Vector128.Int32.2"] = DuplicateSelectedScalarToVector64_Vector128_Int32_2,
                ["DuplicateSelectedScalarToVector64.Vector128.SByte.8"] = DuplicateSelectedScalarToVector64_Vector128_SByte_8,
                ["DuplicateSelectedScalarToVector64.Vector128.Single.2"] = DuplicateSelectedScalarToVector64_Vector128_Single_2,
                ["DuplicateSelectedScalarToVector64.Vector128.UInt16.4"] = DuplicateSelectedScalarToVector64_Vector128_UInt16_4,
                ["DuplicateSelectedScalarToVector64.Vector128.UInt32.2"] = DuplicateSelectedScalarToVector64_Vector128_UInt32_2,
            };
        }
    }
}
