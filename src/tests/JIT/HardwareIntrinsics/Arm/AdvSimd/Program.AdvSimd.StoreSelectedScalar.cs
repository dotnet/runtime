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
                ["StoreSelectedScalar.Vector64.Byte.7"] = StoreSelectedScalar_Vector64_Byte_7,
                ["StoreSelectedScalar.Vector64.Int16.3"] = StoreSelectedScalar_Vector64_Int16_3,
                ["StoreSelectedScalar.Vector64.Int32.1"] = StoreSelectedScalar_Vector64_Int32_1,
                ["StoreSelectedScalar.Vector64.SByte.7"] = StoreSelectedScalar_Vector64_SByte_7,
                ["StoreSelectedScalar.Vector64.Single.1"] = StoreSelectedScalar_Vector64_Single_1,
                ["StoreSelectedScalar.Vector64.UInt16.3"] = StoreSelectedScalar_Vector64_UInt16_3,
                ["StoreSelectedScalar.Vector64.UInt32.1"] = StoreSelectedScalar_Vector64_UInt32_1,
                ["StoreSelectedScalar.Vector128.Byte.15"] = StoreSelectedScalar_Vector128_Byte_15,
                ["StoreSelectedScalar.Vector128.Double.1"] = StoreSelectedScalar_Vector128_Double_1,
                ["StoreSelectedScalar.Vector128.Int16.7"] = StoreSelectedScalar_Vector128_Int16_7,
                ["StoreSelectedScalar.Vector128.Int32.3"] = StoreSelectedScalar_Vector128_Int32_3,
                ["StoreSelectedScalar.Vector128.Int64.1"] = StoreSelectedScalar_Vector128_Int64_1,
                ["StoreSelectedScalar.Vector128.SByte.15"] = StoreSelectedScalar_Vector128_SByte_15,
                ["StoreSelectedScalar.Vector128.Single.3"] = StoreSelectedScalar_Vector128_Single_3,
                ["StoreSelectedScalar.Vector128.UInt16.7"] = StoreSelectedScalar_Vector128_UInt16_7,
                ["StoreSelectedScalar.Vector128.UInt32.3"] = StoreSelectedScalar_Vector128_UInt32_3,
                ["StoreSelectedScalar.Vector128.UInt64.1"] = StoreSelectedScalar_Vector128_UInt64_1,
            };
        }
    }
}
