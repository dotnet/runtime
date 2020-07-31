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
                ["Store.Vector64.Byte"] = Store_Vector64_Byte,
                ["Store.Vector64.Double"] = Store_Vector64_Double,
                ["Store.Vector64.Int16"] = Store_Vector64_Int16,
                ["Store.Vector64.Int32"] = Store_Vector64_Int32,
                ["Store.Vector64.Int64"] = Store_Vector64_Int64,
                ["Store.Vector64.SByte"] = Store_Vector64_SByte,
                ["Store.Vector64.Single"] = Store_Vector64_Single,
                ["Store.Vector64.UInt16"] = Store_Vector64_UInt16,
                ["Store.Vector64.UInt32"] = Store_Vector64_UInt32,
                ["Store.Vector64.UInt64"] = Store_Vector64_UInt64,
                ["Store.Vector128.Byte"] = Store_Vector128_Byte,
                ["Store.Vector128.Double"] = Store_Vector128_Double,
                ["Store.Vector128.Int16"] = Store_Vector128_Int16,
                ["Store.Vector128.Int32"] = Store_Vector128_Int32,
                ["Store.Vector128.Int64"] = Store_Vector128_Int64,
                ["Store.Vector128.SByte"] = Store_Vector128_SByte,
                ["Store.Vector128.Single"] = Store_Vector128_Single,
                ["Store.Vector128.UInt16"] = Store_Vector128_UInt16,
                ["Store.Vector128.UInt32"] = Store_Vector128_UInt32,
                ["Store.Vector128.UInt64"] = Store_Vector128_UInt64,
            };
        }
    }
}
