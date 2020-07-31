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
                ["MultiplyAdd.Vector64.Byte"] = MultiplyAdd_Vector64_Byte,
                ["MultiplyAdd.Vector64.Int16"] = MultiplyAdd_Vector64_Int16,
                ["MultiplyAdd.Vector64.Int32"] = MultiplyAdd_Vector64_Int32,
                ["MultiplyAdd.Vector64.SByte"] = MultiplyAdd_Vector64_SByte,
                ["MultiplyAdd.Vector64.UInt16"] = MultiplyAdd_Vector64_UInt16,
                ["MultiplyAdd.Vector64.UInt32"] = MultiplyAdd_Vector64_UInt32,
                ["MultiplyAdd.Vector128.Byte"] = MultiplyAdd_Vector128_Byte,
                ["MultiplyAdd.Vector128.Int16"] = MultiplyAdd_Vector128_Int16,
                ["MultiplyAdd.Vector128.Int32"] = MultiplyAdd_Vector128_Int32,
                ["MultiplyAdd.Vector128.SByte"] = MultiplyAdd_Vector128_SByte,
                ["MultiplyAdd.Vector128.UInt16"] = MultiplyAdd_Vector128_UInt16,
                ["MultiplyAdd.Vector128.UInt32"] = MultiplyAdd_Vector128_UInt32,
            };
        }
    }
}
