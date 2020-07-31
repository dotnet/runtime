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
                ["AddHighNarrowingLower.Vector64.Byte"] = AddHighNarrowingLower_Vector64_Byte,
                ["AddHighNarrowingLower.Vector64.Int16"] = AddHighNarrowingLower_Vector64_Int16,
                ["AddHighNarrowingLower.Vector64.Int32"] = AddHighNarrowingLower_Vector64_Int32,
                ["AddHighNarrowingLower.Vector64.SByte"] = AddHighNarrowingLower_Vector64_SByte,
                ["AddHighNarrowingLower.Vector64.UInt16"] = AddHighNarrowingLower_Vector64_UInt16,
                ["AddHighNarrowingLower.Vector64.UInt32"] = AddHighNarrowingLower_Vector64_UInt32,
            };
        }
    }
}
