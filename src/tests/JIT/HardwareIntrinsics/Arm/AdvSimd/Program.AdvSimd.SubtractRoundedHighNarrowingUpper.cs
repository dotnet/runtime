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
                ["SubtractRoundedHighNarrowingUpper.Vector128.Byte"] = SubtractRoundedHighNarrowingUpper_Vector128_Byte,
                ["SubtractRoundedHighNarrowingUpper.Vector128.Int16"] = SubtractRoundedHighNarrowingUpper_Vector128_Int16,
                ["SubtractRoundedHighNarrowingUpper.Vector128.Int32"] = SubtractRoundedHighNarrowingUpper_Vector128_Int32,
                ["SubtractRoundedHighNarrowingUpper.Vector128.SByte"] = SubtractRoundedHighNarrowingUpper_Vector128_SByte,
                ["SubtractRoundedHighNarrowingUpper.Vector128.UInt16"] = SubtractRoundedHighNarrowingUpper_Vector128_UInt16,
                ["SubtractRoundedHighNarrowingUpper.Vector128.UInt32"] = SubtractRoundedHighNarrowingUpper_Vector128_UInt32,
            };
        }
    }
}
