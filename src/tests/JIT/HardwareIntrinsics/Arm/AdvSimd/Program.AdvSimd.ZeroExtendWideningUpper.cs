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
                ["ZeroExtendWideningUpper.Vector128.Byte"] = ZeroExtendWideningUpper_Vector128_Byte,
                ["ZeroExtendWideningUpper.Vector128.Int16"] = ZeroExtendWideningUpper_Vector128_Int16,
                ["ZeroExtendWideningUpper.Vector128.Int32"] = ZeroExtendWideningUpper_Vector128_Int32,
                ["ZeroExtendWideningUpper.Vector128.SByte"] = ZeroExtendWideningUpper_Vector128_SByte,
                ["ZeroExtendWideningUpper.Vector128.UInt16"] = ZeroExtendWideningUpper_Vector128_UInt16,
                ["ZeroExtendWideningUpper.Vector128.UInt32"] = ZeroExtendWideningUpper_Vector128_UInt32,
            };
        }
    }
}
