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
                ["MultiplyWideningUpperAndAdd.Vector128.Byte"] = MultiplyWideningUpperAndAdd_Vector128_Byte,
                ["MultiplyWideningUpperAndAdd.Vector128.Int16"] = MultiplyWideningUpperAndAdd_Vector128_Int16,
                ["MultiplyWideningUpperAndAdd.Vector128.Int32"] = MultiplyWideningUpperAndAdd_Vector128_Int32,
                ["MultiplyWideningUpperAndAdd.Vector128.SByte"] = MultiplyWideningUpperAndAdd_Vector128_SByte,
                ["MultiplyWideningUpperAndAdd.Vector128.UInt16"] = MultiplyWideningUpperAndAdd_Vector128_UInt16,
                ["MultiplyWideningUpperAndAdd.Vector128.UInt32"] = MultiplyWideningUpperAndAdd_Vector128_UInt32,
            };
        }
    }
}
