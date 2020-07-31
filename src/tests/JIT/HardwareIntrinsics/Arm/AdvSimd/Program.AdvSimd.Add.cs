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
                ["Add.Vector64.Byte"] = Add_Vector64_Byte,
                ["Add.Vector64.Int16"] = Add_Vector64_Int16,
                ["Add.Vector64.Int32"] = Add_Vector64_Int32,
                ["Add.Vector64.SByte"] = Add_Vector64_SByte,
                ["Add.Vector64.Single"] = Add_Vector64_Single,
                ["Add.Vector64.UInt16"] = Add_Vector64_UInt16,
                ["Add.Vector64.UInt32"] = Add_Vector64_UInt32,
                ["Add.Vector128.Byte"] = Add_Vector128_Byte,
                ["Add.Vector128.Int16"] = Add_Vector128_Int16,
                ["Add.Vector128.Int32"] = Add_Vector128_Int32,
                ["Add.Vector128.Int64"] = Add_Vector128_Int64,
                ["Add.Vector128.SByte"] = Add_Vector128_SByte,
                ["Add.Vector128.Single"] = Add_Vector128_Single,
                ["Add.Vector128.UInt16"] = Add_Vector128_UInt16,
                ["Add.Vector128.UInt32"] = Add_Vector128_UInt32,
                ["Add.Vector128.UInt64"] = Add_Vector128_UInt64,
            };
        }
    }
}
