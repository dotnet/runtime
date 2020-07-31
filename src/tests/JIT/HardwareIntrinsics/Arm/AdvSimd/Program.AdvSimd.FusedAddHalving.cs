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
                ["FusedAddHalving.Vector64.Byte"] = FusedAddHalving_Vector64_Byte,
                ["FusedAddHalving.Vector64.Int16"] = FusedAddHalving_Vector64_Int16,
                ["FusedAddHalving.Vector64.Int32"] = FusedAddHalving_Vector64_Int32,
                ["FusedAddHalving.Vector64.SByte"] = FusedAddHalving_Vector64_SByte,
                ["FusedAddHalving.Vector64.UInt16"] = FusedAddHalving_Vector64_UInt16,
                ["FusedAddHalving.Vector64.UInt32"] = FusedAddHalving_Vector64_UInt32,
                ["FusedAddHalving.Vector128.Byte"] = FusedAddHalving_Vector128_Byte,
                ["FusedAddHalving.Vector128.Int16"] = FusedAddHalving_Vector128_Int16,
                ["FusedAddHalving.Vector128.Int32"] = FusedAddHalving_Vector128_Int32,
                ["FusedAddHalving.Vector128.SByte"] = FusedAddHalving_Vector128_SByte,
                ["FusedAddHalving.Vector128.UInt16"] = FusedAddHalving_Vector128_UInt16,
                ["FusedAddHalving.Vector128.UInt32"] = FusedAddHalving_Vector128_UInt32,
            };
        }
    }
}
