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
                ["Min.Vector64.Byte"] = Min_Vector64_Byte,
                ["Min.Vector64.Int16"] = Min_Vector64_Int16,
                ["Min.Vector64.Int32"] = Min_Vector64_Int32,
                ["Min.Vector64.SByte"] = Min_Vector64_SByte,
                ["Min.Vector64.Single"] = Min_Vector64_Single,
                ["Min.Vector64.UInt16"] = Min_Vector64_UInt16,
                ["Min.Vector64.UInt32"] = Min_Vector64_UInt32,
                ["Min.Vector128.Byte"] = Min_Vector128_Byte,
                ["Min.Vector128.Int16"] = Min_Vector128_Int16,
                ["Min.Vector128.Int32"] = Min_Vector128_Int32,
                ["Min.Vector128.SByte"] = Min_Vector128_SByte,
                ["Min.Vector128.Single"] = Min_Vector128_Single,
                ["Min.Vector128.UInt16"] = Min_Vector128_UInt16,
                ["Min.Vector128.UInt32"] = Min_Vector128_UInt32,
            };
        }
    }
}
