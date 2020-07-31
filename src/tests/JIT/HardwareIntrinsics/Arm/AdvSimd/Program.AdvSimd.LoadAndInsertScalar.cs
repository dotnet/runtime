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
                ["LoadAndInsertScalar.Vector64.Byte.7"] = LoadAndInsertScalar_Vector64_Byte_7,
                ["LoadAndInsertScalar.Vector64.Int16.3"] = LoadAndInsertScalar_Vector64_Int16_3,
                ["LoadAndInsertScalar.Vector64.Int32.1"] = LoadAndInsertScalar_Vector64_Int32_1,
                ["LoadAndInsertScalar.Vector64.SByte.7"] = LoadAndInsertScalar_Vector64_SByte_7,
                ["LoadAndInsertScalar.Vector64.Single.1"] = LoadAndInsertScalar_Vector64_Single_1,
                ["LoadAndInsertScalar.Vector64.UInt16.3"] = LoadAndInsertScalar_Vector64_UInt16_3,
                ["LoadAndInsertScalar.Vector64.UInt32.1"] = LoadAndInsertScalar_Vector64_UInt32_1,
                ["LoadAndInsertScalar.Vector128.Byte.15"] = LoadAndInsertScalar_Vector128_Byte_15,
                ["LoadAndInsertScalar.Vector128.Double.1"] = LoadAndInsertScalar_Vector128_Double_1,
                ["LoadAndInsertScalar.Vector128.Int16.7"] = LoadAndInsertScalar_Vector128_Int16_7,
                ["LoadAndInsertScalar.Vector128.Int32.3"] = LoadAndInsertScalar_Vector128_Int32_3,
                ["LoadAndInsertScalar.Vector128.Int64.1"] = LoadAndInsertScalar_Vector128_Int64_1,
                ["LoadAndInsertScalar.Vector128.SByte.15"] = LoadAndInsertScalar_Vector128_SByte_15,
                ["LoadAndInsertScalar.Vector128.Single.3"] = LoadAndInsertScalar_Vector128_Single_3,
                ["LoadAndInsertScalar.Vector128.UInt16.7"] = LoadAndInsertScalar_Vector128_UInt16_7,
                ["LoadAndInsertScalar.Vector128.UInt32.3"] = LoadAndInsertScalar_Vector128_UInt32_3,
                ["LoadAndInsertScalar.Vector128.UInt64.1"] = LoadAndInsertScalar_Vector128_UInt64_1,
            };
        }
    }
}
