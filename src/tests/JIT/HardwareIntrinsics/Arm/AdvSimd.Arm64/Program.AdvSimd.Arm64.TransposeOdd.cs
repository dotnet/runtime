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
                ["TransposeOdd.Vector64.Byte"] = TransposeOdd_Vector64_Byte,
                ["TransposeOdd.Vector64.Int16"] = TransposeOdd_Vector64_Int16,
                ["TransposeOdd.Vector64.Int32"] = TransposeOdd_Vector64_Int32,
                ["TransposeOdd.Vector64.SByte"] = TransposeOdd_Vector64_SByte,
                ["TransposeOdd.Vector64.Single"] = TransposeOdd_Vector64_Single,
                ["TransposeOdd.Vector64.UInt16"] = TransposeOdd_Vector64_UInt16,
                ["TransposeOdd.Vector64.UInt32"] = TransposeOdd_Vector64_UInt32,
                ["TransposeOdd.Vector128.Byte"] = TransposeOdd_Vector128_Byte,
                ["TransposeOdd.Vector128.Double"] = TransposeOdd_Vector128_Double,
                ["TransposeOdd.Vector128.Int16"] = TransposeOdd_Vector128_Int16,
                ["TransposeOdd.Vector128.Int32"] = TransposeOdd_Vector128_Int32,
                ["TransposeOdd.Vector128.Int64"] = TransposeOdd_Vector128_Int64,
                ["TransposeOdd.Vector128.SByte"] = TransposeOdd_Vector128_SByte,
                ["TransposeOdd.Vector128.Single"] = TransposeOdd_Vector128_Single,
                ["TransposeOdd.Vector128.UInt16"] = TransposeOdd_Vector128_UInt16,
                ["TransposeOdd.Vector128.UInt32"] = TransposeOdd_Vector128_UInt32,
                ["TransposeOdd.Vector128.UInt64"] = TransposeOdd_Vector128_UInt64,
            };
        }
    }
}
