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
                ["SubtractRoundedHighNarrowingLower.Vector64.Byte"] = SubtractRoundedHighNarrowingLower_Vector64_Byte,
                ["SubtractRoundedHighNarrowingLower.Vector64.Int16"] = SubtractRoundedHighNarrowingLower_Vector64_Int16,
                ["SubtractRoundedHighNarrowingLower.Vector64.Int32"] = SubtractRoundedHighNarrowingLower_Vector64_Int32,
                ["SubtractRoundedHighNarrowingLower.Vector64.SByte"] = SubtractRoundedHighNarrowingLower_Vector64_SByte,
                ["SubtractRoundedHighNarrowingLower.Vector64.UInt16"] = SubtractRoundedHighNarrowingLower_Vector64_UInt16,
                ["SubtractRoundedHighNarrowingLower.Vector64.UInt32"] = SubtractRoundedHighNarrowingLower_Vector64_UInt32,
            };
        }
    }
}
