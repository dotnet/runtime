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
                ["UnzipOdd.Vector64.Byte"] = UnzipOdd_Vector64_Byte,
                ["UnzipOdd.Vector64.Int16"] = UnzipOdd_Vector64_Int16,
                ["UnzipOdd.Vector64.Int32"] = UnzipOdd_Vector64_Int32,
                ["UnzipOdd.Vector64.SByte"] = UnzipOdd_Vector64_SByte,
                ["UnzipOdd.Vector64.Single"] = UnzipOdd_Vector64_Single,
                ["UnzipOdd.Vector64.UInt16"] = UnzipOdd_Vector64_UInt16,
                ["UnzipOdd.Vector64.UInt32"] = UnzipOdd_Vector64_UInt32,
                ["UnzipOdd.Vector128.Byte"] = UnzipOdd_Vector128_Byte,
                ["UnzipOdd.Vector128.Double"] = UnzipOdd_Vector128_Double,
                ["UnzipOdd.Vector128.Int16"] = UnzipOdd_Vector128_Int16,
                ["UnzipOdd.Vector128.Int32"] = UnzipOdd_Vector128_Int32,
                ["UnzipOdd.Vector128.Int64"] = UnzipOdd_Vector128_Int64,
                ["UnzipOdd.Vector128.SByte"] = UnzipOdd_Vector128_SByte,
                ["UnzipOdd.Vector128.Single"] = UnzipOdd_Vector128_Single,
                ["UnzipOdd.Vector128.UInt16"] = UnzipOdd_Vector128_UInt16,
                ["UnzipOdd.Vector128.UInt32"] = UnzipOdd_Vector128_UInt32,
                ["UnzipOdd.Vector128.UInt64"] = UnzipOdd_Vector128_UInt64,
            };
        }
    }
}
