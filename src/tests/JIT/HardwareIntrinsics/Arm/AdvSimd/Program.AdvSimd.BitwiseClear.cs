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
                ["BitwiseClear.Vector64.Byte"] = BitwiseClear_Vector64_Byte,
                ["BitwiseClear.Vector64.Double"] = BitwiseClear_Vector64_Double,
                ["BitwiseClear.Vector64.Int16"] = BitwiseClear_Vector64_Int16,
                ["BitwiseClear.Vector64.Int32"] = BitwiseClear_Vector64_Int32,
                ["BitwiseClear.Vector64.Int64"] = BitwiseClear_Vector64_Int64,
                ["BitwiseClear.Vector64.SByte"] = BitwiseClear_Vector64_SByte,
                ["BitwiseClear.Vector64.Single"] = BitwiseClear_Vector64_Single,
                ["BitwiseClear.Vector64.UInt16"] = BitwiseClear_Vector64_UInt16,
                ["BitwiseClear.Vector64.UInt32"] = BitwiseClear_Vector64_UInt32,
                ["BitwiseClear.Vector64.UInt64"] = BitwiseClear_Vector64_UInt64,
                ["BitwiseClear.Vector128.Byte"] = BitwiseClear_Vector128_Byte,
                ["BitwiseClear.Vector128.Double"] = BitwiseClear_Vector128_Double,
                ["BitwiseClear.Vector128.Int16"] = BitwiseClear_Vector128_Int16,
                ["BitwiseClear.Vector128.Int32"] = BitwiseClear_Vector128_Int32,
                ["BitwiseClear.Vector128.Int64"] = BitwiseClear_Vector128_Int64,
                ["BitwiseClear.Vector128.SByte"] = BitwiseClear_Vector128_SByte,
                ["BitwiseClear.Vector128.Single"] = BitwiseClear_Vector128_Single,
                ["BitwiseClear.Vector128.UInt16"] = BitwiseClear_Vector128_UInt16,
                ["BitwiseClear.Vector128.UInt32"] = BitwiseClear_Vector128_UInt32,
                ["BitwiseClear.Vector128.UInt64"] = BitwiseClear_Vector128_UInt64,
            };
        }
    }
}
