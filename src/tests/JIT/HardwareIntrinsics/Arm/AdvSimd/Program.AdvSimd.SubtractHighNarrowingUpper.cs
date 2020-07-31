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
                ["SubtractHighNarrowingUpper.Vector128.Byte"] = SubtractHighNarrowingUpper_Vector128_Byte,
                ["SubtractHighNarrowingUpper.Vector128.Int16"] = SubtractHighNarrowingUpper_Vector128_Int16,
                ["SubtractHighNarrowingUpper.Vector128.Int32"] = SubtractHighNarrowingUpper_Vector128_Int32,
                ["SubtractHighNarrowingUpper.Vector128.SByte"] = SubtractHighNarrowingUpper_Vector128_SByte,
                ["SubtractHighNarrowingUpper.Vector128.UInt16"] = SubtractHighNarrowingUpper_Vector128_UInt16,
                ["SubtractHighNarrowingUpper.Vector128.UInt32"] = SubtractHighNarrowingUpper_Vector128_UInt32,
            };
        }
    }
}
