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
                ["InsertSelectedScalar.Vector64.Byte.7.Vector64.Byte.7"] = InsertSelectedScalar_Vector64_Byte_7_Vector64_Byte_7,
                ["InsertSelectedScalar.Vector64.Byte.7.Vector128.Byte.15"] = InsertSelectedScalar_Vector64_Byte_7_Vector128_Byte_15,
                ["InsertSelectedScalar.Vector64.Int16.3.Vector64.Int16.3"] = InsertSelectedScalar_Vector64_Int16_3_Vector64_Int16_3,
                ["InsertSelectedScalar.Vector64.Int16.3.Vector128.Int16.7"] = InsertSelectedScalar_Vector64_Int16_3_Vector128_Int16_7,
                ["InsertSelectedScalar.Vector64.Int32.1.Vector64.Int32.1"] = InsertSelectedScalar_Vector64_Int32_1_Vector64_Int32_1,
                ["InsertSelectedScalar.Vector64.Int32.1.Vector128.Int32.3"] = InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3,
                ["InsertSelectedScalar.Vector64.SByte.7.Vector64.SByte.7"] = InsertSelectedScalar_Vector64_SByte_7_Vector64_SByte_7,
                ["InsertSelectedScalar.Vector64.SByte.7.Vector128.SByte.15"] = InsertSelectedScalar_Vector64_SByte_7_Vector128_SByte_15,
                ["InsertSelectedScalar.Vector64.Single.1.Vector64.Single.1"] = InsertSelectedScalar_Vector64_Single_1_Vector64_Single_1,
                ["InsertSelectedScalar.Vector64.Single.1.Vector128.Single.3"] = InsertSelectedScalar_Vector64_Single_1_Vector128_Single_3,
                ["InsertSelectedScalar.Vector64.UInt16.3.Vector64.UInt16.3"] = InsertSelectedScalar_Vector64_UInt16_3_Vector64_UInt16_3,
                ["InsertSelectedScalar.Vector64.UInt16.3.Vector128.UInt16.7"] = InsertSelectedScalar_Vector64_UInt16_3_Vector128_UInt16_7,
                ["InsertSelectedScalar.Vector64.UInt32.1.Vector64.UInt32.1"] = InsertSelectedScalar_Vector64_UInt32_1_Vector64_UInt32_1,
                ["InsertSelectedScalar.Vector64.UInt32.1.Vector128.UInt32.3"] = InsertSelectedScalar_Vector64_UInt32_1_Vector128_UInt32_3,
                ["InsertSelectedScalar.Vector128.Byte.15.Vector64.Byte.7"] = InsertSelectedScalar_Vector128_Byte_15_Vector64_Byte_7,
                ["InsertSelectedScalar.Vector128.Byte.15.Vector128.Byte.15"] = InsertSelectedScalar_Vector128_Byte_15_Vector128_Byte_15,
                ["InsertSelectedScalar.Vector128.Double.1.Vector128.Double.1"] = InsertSelectedScalar_Vector128_Double_1_Vector128_Double_1,
                ["InsertSelectedScalar.Vector128.Int16.7.Vector64.Int16.3"] = InsertSelectedScalar_Vector128_Int16_7_Vector64_Int16_3,
                ["InsertSelectedScalar.Vector128.Int16.7.Vector128.Int16.7"] = InsertSelectedScalar_Vector128_Int16_7_Vector128_Int16_7,
                ["InsertSelectedScalar.Vector128.Int32.3.Vector64.Int32.1"] = InsertSelectedScalar_Vector128_Int32_3_Vector64_Int32_1,
                ["InsertSelectedScalar.Vector128.Int32.3.Vector128.Int32.3"] = InsertSelectedScalar_Vector128_Int32_3_Vector128_Int32_3,
                ["InsertSelectedScalar.Vector128.Int64.1.Vector128.Int64.1"] = InsertSelectedScalar_Vector128_Int64_1_Vector128_Int64_1,
                ["InsertSelectedScalar.Vector128.SByte.15.Vector64.SByte.7"] = InsertSelectedScalar_Vector128_SByte_15_Vector64_SByte_7,
                ["InsertSelectedScalar.Vector128.SByte.15.Vector128.SByte.15"] = InsertSelectedScalar_Vector128_SByte_15_Vector128_SByte_15,
                ["InsertSelectedScalar.Vector128.Single.3.Vector64.Single.1"] = InsertSelectedScalar_Vector128_Single_3_Vector64_Single_1,
                ["InsertSelectedScalar.Vector128.Single.3.Vector128.Single.3"] = InsertSelectedScalar_Vector128_Single_3_Vector128_Single_3,
                ["InsertSelectedScalar.Vector128.UInt16.7.Vector64.UInt16.3"] = InsertSelectedScalar_Vector128_UInt16_7_Vector64_UInt16_3,
                ["InsertSelectedScalar.Vector128.UInt16.7.Vector128.UInt16.7"] = InsertSelectedScalar_Vector128_UInt16_7_Vector128_UInt16_7,
                ["InsertSelectedScalar.Vector128.UInt32.3.Vector64.UInt32.1"] = InsertSelectedScalar_Vector128_UInt32_3_Vector64_UInt32_1,
                ["InsertSelectedScalar.Vector128.UInt32.3.Vector128.UInt32.3"] = InsertSelectedScalar_Vector128_UInt32_3_Vector128_UInt32_3,
                ["InsertSelectedScalar.Vector128.UInt64.1.Vector128.UInt64.1"] = InsertSelectedScalar_Vector128_UInt64_1_Vector128_UInt64_1,
            };
        }
    }
}
