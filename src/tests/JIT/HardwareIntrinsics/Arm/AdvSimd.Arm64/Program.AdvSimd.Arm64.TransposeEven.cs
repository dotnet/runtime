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
                ["TransposeEven.Vector64.Byte"] = TransposeEven_Vector64_Byte,
                ["TransposeEven.Vector64.Int16"] = TransposeEven_Vector64_Int16,
                ["TransposeEven.Vector64.Int32"] = TransposeEven_Vector64_Int32,
                ["TransposeEven.Vector64.SByte"] = TransposeEven_Vector64_SByte,
                ["TransposeEven.Vector64.Single"] = TransposeEven_Vector64_Single,
                ["TransposeEven.Vector64.UInt16"] = TransposeEven_Vector64_UInt16,
                ["TransposeEven.Vector64.UInt32"] = TransposeEven_Vector64_UInt32,
                ["TransposeEven.Vector128.Byte"] = TransposeEven_Vector128_Byte,
                ["TransposeEven.Vector128.Double"] = TransposeEven_Vector128_Double,
                ["TransposeEven.Vector128.Int16"] = TransposeEven_Vector128_Int16,
                ["TransposeEven.Vector128.Int32"] = TransposeEven_Vector128_Int32,
                ["TransposeEven.Vector128.Int64"] = TransposeEven_Vector128_Int64,
                ["TransposeEven.Vector128.SByte"] = TransposeEven_Vector128_SByte,
                ["TransposeEven.Vector128.Single"] = TransposeEven_Vector128_Single,
                ["TransposeEven.Vector128.UInt16"] = TransposeEven_Vector128_UInt16,
                ["TransposeEven.Vector128.UInt32"] = TransposeEven_Vector128_UInt32,
                ["TransposeEven.Vector128.UInt64"] = TransposeEven_Vector128_UInt64,
            };
        }
    }
}
