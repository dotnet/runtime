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
                ["MultiplySubtract.Vector64.Byte"] = MultiplySubtract_Vector64_Byte,
                ["MultiplySubtract.Vector64.Int16"] = MultiplySubtract_Vector64_Int16,
                ["MultiplySubtract.Vector64.Int32"] = MultiplySubtract_Vector64_Int32,
                ["MultiplySubtract.Vector64.SByte"] = MultiplySubtract_Vector64_SByte,
                ["MultiplySubtract.Vector64.UInt16"] = MultiplySubtract_Vector64_UInt16,
                ["MultiplySubtract.Vector64.UInt32"] = MultiplySubtract_Vector64_UInt32,
                ["MultiplySubtract.Vector128.Byte"] = MultiplySubtract_Vector128_Byte,
                ["MultiplySubtract.Vector128.Int16"] = MultiplySubtract_Vector128_Int16,
                ["MultiplySubtract.Vector128.Int32"] = MultiplySubtract_Vector128_Int32,
                ["MultiplySubtract.Vector128.SByte"] = MultiplySubtract_Vector128_SByte,
                ["MultiplySubtract.Vector128.UInt16"] = MultiplySubtract_Vector128_UInt16,
                ["MultiplySubtract.Vector128.UInt32"] = MultiplySubtract_Vector128_UInt32,
            };
        }
    }
}
