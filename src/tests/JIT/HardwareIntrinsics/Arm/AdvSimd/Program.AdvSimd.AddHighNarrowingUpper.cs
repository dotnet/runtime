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
                ["AddHighNarrowingUpper.Vector128.Byte"] = AddHighNarrowingUpper_Vector128_Byte,
                ["AddHighNarrowingUpper.Vector128.Int16"] = AddHighNarrowingUpper_Vector128_Int16,
                ["AddHighNarrowingUpper.Vector128.Int32"] = AddHighNarrowingUpper_Vector128_Int32,
                ["AddHighNarrowingUpper.Vector128.SByte"] = AddHighNarrowingUpper_Vector128_SByte,
                ["AddHighNarrowingUpper.Vector128.UInt16"] = AddHighNarrowingUpper_Vector128_UInt16,
                ["AddHighNarrowingUpper.Vector128.UInt32"] = AddHighNarrowingUpper_Vector128_UInt32,
            };
        }
    }
}
