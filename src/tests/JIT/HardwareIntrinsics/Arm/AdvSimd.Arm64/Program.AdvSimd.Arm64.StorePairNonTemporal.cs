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
                ["StorePairNonTemporal.Vector64.Byte"] = StorePairNonTemporal_Vector64_Byte,
                ["StorePairNonTemporal.Vector64.Double"] = StorePairNonTemporal_Vector64_Double,
                ["StorePairNonTemporal.Vector64.Int16"] = StorePairNonTemporal_Vector64_Int16,
                ["StorePairNonTemporal.Vector64.Int32"] = StorePairNonTemporal_Vector64_Int32,
                ["StorePairNonTemporal.Vector64.Int64"] = StorePairNonTemporal_Vector64_Int64,
                ["StorePairNonTemporal.Vector64.SByte"] = StorePairNonTemporal_Vector64_SByte,
                ["StorePairNonTemporal.Vector64.Single"] = StorePairNonTemporal_Vector64_Single,
                ["StorePairNonTemporal.Vector64.UInt16"] = StorePairNonTemporal_Vector64_UInt16,
                ["StorePairNonTemporal.Vector64.UInt32"] = StorePairNonTemporal_Vector64_UInt32,
                ["StorePairNonTemporal.Vector64.UInt64"] = StorePairNonTemporal_Vector64_UInt64,
                ["StorePairNonTemporal.Vector128.Byte"] = StorePairNonTemporal_Vector128_Byte,
                ["StorePairNonTemporal.Vector128.Double"] = StorePairNonTemporal_Vector128_Double,
                ["StorePairNonTemporal.Vector128.Int16"] = StorePairNonTemporal_Vector128_Int16,
                ["StorePairNonTemporal.Vector128.Int32"] = StorePairNonTemporal_Vector128_Int32,
                ["StorePairNonTemporal.Vector128.Int64"] = StorePairNonTemporal_Vector128_Int64,
                ["StorePairNonTemporal.Vector128.SByte"] = StorePairNonTemporal_Vector128_SByte,
                ["StorePairNonTemporal.Vector128.Single"] = StorePairNonTemporal_Vector128_Single,
                ["StorePairNonTemporal.Vector128.UInt16"] = StorePairNonTemporal_Vector128_UInt16,
                ["StorePairNonTemporal.Vector128.UInt32"] = StorePairNonTemporal_Vector128_UInt32,
                ["StorePairNonTemporal.Vector128.UInt64"] = StorePairNonTemporal_Vector128_UInt64,
            };
        }
    }
}
