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
                ["StorePair.Vector64.Byte"] = StorePair_Vector64_Byte,
                ["StorePair.Vector64.Double"] = StorePair_Vector64_Double,
                ["StorePair.Vector64.Int16"] = StorePair_Vector64_Int16,
                ["StorePair.Vector64.Int32"] = StorePair_Vector64_Int32,
                ["StorePair.Vector64.Int64"] = StorePair_Vector64_Int64,
                ["StorePair.Vector64.SByte"] = StorePair_Vector64_SByte,
                ["StorePair.Vector64.Single"] = StorePair_Vector64_Single,
                ["StorePair.Vector64.UInt16"] = StorePair_Vector64_UInt16,
                ["StorePair.Vector64.UInt32"] = StorePair_Vector64_UInt32,
                ["StorePair.Vector64.UInt64"] = StorePair_Vector64_UInt64,
                ["StorePair.Vector128.Byte"] = StorePair_Vector128_Byte,
                ["StorePair.Vector128.Double"] = StorePair_Vector128_Double,
                ["StorePair.Vector128.Int16"] = StorePair_Vector128_Int16,
                ["StorePair.Vector128.Int32"] = StorePair_Vector128_Int32,
                ["StorePair.Vector128.Int64"] = StorePair_Vector128_Int64,
                ["StorePair.Vector128.SByte"] = StorePair_Vector128_SByte,
                ["StorePair.Vector128.Single"] = StorePair_Vector128_Single,
                ["StorePair.Vector128.UInt16"] = StorePair_Vector128_UInt16,
                ["StorePair.Vector128.UInt32"] = StorePair_Vector128_UInt32,
                ["StorePair.Vector128.UInt64"] = StorePair_Vector128_UInt64,
            };
        }
    }
}
