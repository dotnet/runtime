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
                ["FusedAddRoundedHalving.Vector64.Byte"] = FusedAddRoundedHalving_Vector64_Byte,
                ["FusedAddRoundedHalving.Vector64.Int16"] = FusedAddRoundedHalving_Vector64_Int16,
                ["FusedAddRoundedHalving.Vector64.Int32"] = FusedAddRoundedHalving_Vector64_Int32,
                ["FusedAddRoundedHalving.Vector64.SByte"] = FusedAddRoundedHalving_Vector64_SByte,
                ["FusedAddRoundedHalving.Vector64.UInt16"] = FusedAddRoundedHalving_Vector64_UInt16,
                ["FusedAddRoundedHalving.Vector64.UInt32"] = FusedAddRoundedHalving_Vector64_UInt32,
                ["FusedAddRoundedHalving.Vector128.Byte"] = FusedAddRoundedHalving_Vector128_Byte,
                ["FusedAddRoundedHalving.Vector128.Int16"] = FusedAddRoundedHalving_Vector128_Int16,
                ["FusedAddRoundedHalving.Vector128.Int32"] = FusedAddRoundedHalving_Vector128_Int32,
                ["FusedAddRoundedHalving.Vector128.SByte"] = FusedAddRoundedHalving_Vector128_SByte,
                ["FusedAddRoundedHalving.Vector128.UInt16"] = FusedAddRoundedHalving_Vector128_UInt16,
                ["FusedAddRoundedHalving.Vector128.UInt32"] = FusedAddRoundedHalving_Vector128_UInt32,
            };
        }
    }
}
