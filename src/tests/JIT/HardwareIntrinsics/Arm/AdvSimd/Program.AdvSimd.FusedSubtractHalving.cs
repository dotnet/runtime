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
                ["FusedSubtractHalving.Vector64.Byte"] = FusedSubtractHalving_Vector64_Byte,
                ["FusedSubtractHalving.Vector64.Int16"] = FusedSubtractHalving_Vector64_Int16,
                ["FusedSubtractHalving.Vector64.Int32"] = FusedSubtractHalving_Vector64_Int32,
                ["FusedSubtractHalving.Vector64.SByte"] = FusedSubtractHalving_Vector64_SByte,
                ["FusedSubtractHalving.Vector64.UInt16"] = FusedSubtractHalving_Vector64_UInt16,
                ["FusedSubtractHalving.Vector64.UInt32"] = FusedSubtractHalving_Vector64_UInt32,
                ["FusedSubtractHalving.Vector128.Byte"] = FusedSubtractHalving_Vector128_Byte,
                ["FusedSubtractHalving.Vector128.Int16"] = FusedSubtractHalving_Vector128_Int16,
                ["FusedSubtractHalving.Vector128.Int32"] = FusedSubtractHalving_Vector128_Int32,
                ["FusedSubtractHalving.Vector128.SByte"] = FusedSubtractHalving_Vector128_SByte,
                ["FusedSubtractHalving.Vector128.UInt16"] = FusedSubtractHalving_Vector128_UInt16,
                ["FusedSubtractHalving.Vector128.UInt32"] = FusedSubtractHalving_Vector128_UInt32,
            };
        }
    }
}
