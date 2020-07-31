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
                ["SubtractHighNarrowingLower.Vector64.Byte"] = SubtractHighNarrowingLower_Vector64_Byte,
                ["SubtractHighNarrowingLower.Vector64.Int16"] = SubtractHighNarrowingLower_Vector64_Int16,
                ["SubtractHighNarrowingLower.Vector64.Int32"] = SubtractHighNarrowingLower_Vector64_Int32,
                ["SubtractHighNarrowingLower.Vector64.SByte"] = SubtractHighNarrowingLower_Vector64_SByte,
                ["SubtractHighNarrowingLower.Vector64.UInt16"] = SubtractHighNarrowingLower_Vector64_UInt16,
                ["SubtractHighNarrowingLower.Vector64.UInt32"] = SubtractHighNarrowingLower_Vector64_UInt32,
            };
        }
    }
}
