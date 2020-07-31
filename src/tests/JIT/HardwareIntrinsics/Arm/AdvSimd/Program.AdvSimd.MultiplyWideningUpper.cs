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
                ["MultiplyWideningUpper.Vector128.Byte"] = MultiplyWideningUpper_Vector128_Byte,
                ["MultiplyWideningUpper.Vector128.Int16"] = MultiplyWideningUpper_Vector128_Int16,
                ["MultiplyWideningUpper.Vector128.Int32"] = MultiplyWideningUpper_Vector128_Int32,
                ["MultiplyWideningUpper.Vector128.SByte"] = MultiplyWideningUpper_Vector128_SByte,
                ["MultiplyWideningUpper.Vector128.UInt16"] = MultiplyWideningUpper_Vector128_UInt16,
                ["MultiplyWideningUpper.Vector128.UInt32"] = MultiplyWideningUpper_Vector128_UInt32,
            };
        }
    }
}
