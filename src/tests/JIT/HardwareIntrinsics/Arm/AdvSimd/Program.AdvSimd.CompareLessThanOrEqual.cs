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
                ["CompareLessThanOrEqual.Vector64.Byte"] = CompareLessThanOrEqual_Vector64_Byte,
                ["CompareLessThanOrEqual.Vector64.Int16"] = CompareLessThanOrEqual_Vector64_Int16,
                ["CompareLessThanOrEqual.Vector64.Int32"] = CompareLessThanOrEqual_Vector64_Int32,
                ["CompareLessThanOrEqual.Vector64.SByte"] = CompareLessThanOrEqual_Vector64_SByte,
                ["CompareLessThanOrEqual.Vector64.Single"] = CompareLessThanOrEqual_Vector64_Single,
                ["CompareLessThanOrEqual.Vector64.UInt16"] = CompareLessThanOrEqual_Vector64_UInt16,
                ["CompareLessThanOrEqual.Vector64.UInt32"] = CompareLessThanOrEqual_Vector64_UInt32,
                ["CompareLessThanOrEqual.Vector128.Byte"] = CompareLessThanOrEqual_Vector128_Byte,
                ["CompareLessThanOrEqual.Vector128.Int16"] = CompareLessThanOrEqual_Vector128_Int16,
                ["CompareLessThanOrEqual.Vector128.Int32"] = CompareLessThanOrEqual_Vector128_Int32,
                ["CompareLessThanOrEqual.Vector128.SByte"] = CompareLessThanOrEqual_Vector128_SByte,
                ["CompareLessThanOrEqual.Vector128.Single"] = CompareLessThanOrEqual_Vector128_Single,
                ["CompareLessThanOrEqual.Vector128.UInt16"] = CompareLessThanOrEqual_Vector128_UInt16,
                ["CompareLessThanOrEqual.Vector128.UInt32"] = CompareLessThanOrEqual_Vector128_UInt32,
            };
        }
    }
}
