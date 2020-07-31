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
                ["Max.Vector64.Byte"] = Max_Vector64_Byte,
                ["Max.Vector64.Int16"] = Max_Vector64_Int16,
                ["Max.Vector64.Int32"] = Max_Vector64_Int32,
                ["Max.Vector64.SByte"] = Max_Vector64_SByte,
                ["Max.Vector64.Single"] = Max_Vector64_Single,
                ["Max.Vector64.UInt16"] = Max_Vector64_UInt16,
                ["Max.Vector64.UInt32"] = Max_Vector64_UInt32,
                ["Max.Vector128.Byte"] = Max_Vector128_Byte,
                ["Max.Vector128.Int16"] = Max_Vector128_Int16,
                ["Max.Vector128.Int32"] = Max_Vector128_Int32,
                ["Max.Vector128.SByte"] = Max_Vector128_SByte,
                ["Max.Vector128.Single"] = Max_Vector128_Single,
                ["Max.Vector128.UInt16"] = Max_Vector128_UInt16,
                ["Max.Vector128.UInt32"] = Max_Vector128_UInt32,
            };
        }
    }
}
