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
            TestList = new Dictionary<string, Action>() {
                ["ZipHigh.Vector64.Byte"] = ZipHigh_Vector64_Byte,
                ["ZipHigh.Vector64.Int16"] = ZipHigh_Vector64_Int16,
                ["ZipHigh.Vector64.Int32"] = ZipHigh_Vector64_Int32,
                ["ZipHigh.Vector64.SByte"] = ZipHigh_Vector64_SByte,
                ["ZipHigh.Vector64.Single"] = ZipHigh_Vector64_Single,
                ["ZipHigh.Vector64.UInt16"] = ZipHigh_Vector64_UInt16,
                ["ZipHigh.Vector64.UInt32"] = ZipHigh_Vector64_UInt32,
                ["ZipHigh.Vector128.Byte"] = ZipHigh_Vector128_Byte,
                ["ZipHigh.Vector128.Double"] = ZipHigh_Vector128_Double,
                ["ZipHigh.Vector128.Int16"] = ZipHigh_Vector128_Int16,
                ["ZipHigh.Vector128.Int32"] = ZipHigh_Vector128_Int32,
                ["ZipHigh.Vector128.Int64"] = ZipHigh_Vector128_Int64,
                ["ZipHigh.Vector128.SByte"] = ZipHigh_Vector128_SByte,
                ["ZipHigh.Vector128.Single"] = ZipHigh_Vector128_Single,
                ["ZipHigh.Vector128.UInt16"] = ZipHigh_Vector128_UInt16,
                ["ZipHigh.Vector128.UInt32"] = ZipHigh_Vector128_UInt32,
                ["ZipHigh.Vector128.UInt64"] = ZipHigh_Vector128_UInt64,
                ["ZipLow.Vector64.Byte"] = ZipLow_Vector64_Byte,
                ["ZipLow.Vector64.Int16"] = ZipLow_Vector64_Int16,
                ["ZipLow.Vector64.Int32"] = ZipLow_Vector64_Int32,
                ["ZipLow.Vector64.SByte"] = ZipLow_Vector64_SByte,
                ["ZipLow.Vector64.Single"] = ZipLow_Vector64_Single,
                ["ZipLow.Vector64.UInt16"] = ZipLow_Vector64_UInt16,
                ["ZipLow.Vector64.UInt32"] = ZipLow_Vector64_UInt32,
                ["ZipLow.Vector128.Byte"] = ZipLow_Vector128_Byte,
                ["ZipLow.Vector128.Double"] = ZipLow_Vector128_Double,
                ["ZipLow.Vector128.Int16"] = ZipLow_Vector128_Int16,
                ["ZipLow.Vector128.Int32"] = ZipLow_Vector128_Int32,
                ["ZipLow.Vector128.Int64"] = ZipLow_Vector128_Int64,
                ["ZipLow.Vector128.SByte"] = ZipLow_Vector128_SByte,
                ["ZipLow.Vector128.Single"] = ZipLow_Vector128_Single,
                ["ZipLow.Vector128.UInt16"] = ZipLow_Vector128_UInt16,
                ["ZipLow.Vector128.UInt32"] = ZipLow_Vector128_UInt32,
                ["ZipLow.Vector128.UInt64"] = ZipLow_Vector128_UInt64,
            };
        }
    }
}
