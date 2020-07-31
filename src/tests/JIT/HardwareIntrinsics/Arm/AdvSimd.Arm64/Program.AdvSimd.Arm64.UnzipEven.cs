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
                ["UnzipEven.Vector64.Byte"] = UnzipEven_Vector64_Byte,
                ["UnzipEven.Vector64.Int16"] = UnzipEven_Vector64_Int16,
                ["UnzipEven.Vector64.Int32"] = UnzipEven_Vector64_Int32,
                ["UnzipEven.Vector64.SByte"] = UnzipEven_Vector64_SByte,
                ["UnzipEven.Vector64.Single"] = UnzipEven_Vector64_Single,
                ["UnzipEven.Vector64.UInt16"] = UnzipEven_Vector64_UInt16,
                ["UnzipEven.Vector64.UInt32"] = UnzipEven_Vector64_UInt32,
                ["UnzipEven.Vector128.Byte"] = UnzipEven_Vector128_Byte,
                ["UnzipEven.Vector128.Double"] = UnzipEven_Vector128_Double,
                ["UnzipEven.Vector128.Int16"] = UnzipEven_Vector128_Int16,
                ["UnzipEven.Vector128.Int32"] = UnzipEven_Vector128_Int32,
                ["UnzipEven.Vector128.Int64"] = UnzipEven_Vector128_Int64,
                ["UnzipEven.Vector128.SByte"] = UnzipEven_Vector128_SByte,
                ["UnzipEven.Vector128.Single"] = UnzipEven_Vector128_Single,
                ["UnzipEven.Vector128.UInt16"] = UnzipEven_Vector128_UInt16,
                ["UnzipEven.Vector128.UInt32"] = UnzipEven_Vector128_UInt32,
                ["UnzipEven.Vector128.UInt64"] = UnzipEven_Vector128_UInt64,
            };
        }
    }
}
