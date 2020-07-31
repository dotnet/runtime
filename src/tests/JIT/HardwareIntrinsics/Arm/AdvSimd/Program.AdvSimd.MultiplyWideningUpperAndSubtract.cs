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
                ["MultiplyWideningUpperAndSubtract.Vector128.Byte"] = MultiplyWideningUpperAndSubtract_Vector128_Byte,
                ["MultiplyWideningUpperAndSubtract.Vector128.Int16"] = MultiplyWideningUpperAndSubtract_Vector128_Int16,
                ["MultiplyWideningUpperAndSubtract.Vector128.Int32"] = MultiplyWideningUpperAndSubtract_Vector128_Int32,
                ["MultiplyWideningUpperAndSubtract.Vector128.SByte"] = MultiplyWideningUpperAndSubtract_Vector128_SByte,
                ["MultiplyWideningUpperAndSubtract.Vector128.UInt16"] = MultiplyWideningUpperAndSubtract_Vector128_UInt16,
                ["MultiplyWideningUpperAndSubtract.Vector128.UInt32"] = MultiplyWideningUpperAndSubtract_Vector128_UInt32,
            };
        }
    }
}
