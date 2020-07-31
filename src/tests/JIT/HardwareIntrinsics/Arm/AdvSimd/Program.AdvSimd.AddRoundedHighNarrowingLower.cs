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
                ["AddRoundedHighNarrowingLower.Vector64.Byte"] = AddRoundedHighNarrowingLower_Vector64_Byte,
                ["AddRoundedHighNarrowingLower.Vector64.Int16"] = AddRoundedHighNarrowingLower_Vector64_Int16,
                ["AddRoundedHighNarrowingLower.Vector64.Int32"] = AddRoundedHighNarrowingLower_Vector64_Int32,
                ["AddRoundedHighNarrowingLower.Vector64.SByte"] = AddRoundedHighNarrowingLower_Vector64_SByte,
                ["AddRoundedHighNarrowingLower.Vector64.UInt16"] = AddRoundedHighNarrowingLower_Vector64_UInt16,
                ["AddRoundedHighNarrowingLower.Vector64.UInt32"] = AddRoundedHighNarrowingLower_Vector64_UInt32,
            };
        }
    }
}
