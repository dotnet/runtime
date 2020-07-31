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
                ["AddRoundedHighNarrowingUpper.Vector128.Byte"] = AddRoundedHighNarrowingUpper_Vector128_Byte,
                ["AddRoundedHighNarrowingUpper.Vector128.Int16"] = AddRoundedHighNarrowingUpper_Vector128_Int16,
                ["AddRoundedHighNarrowingUpper.Vector128.Int32"] = AddRoundedHighNarrowingUpper_Vector128_Int32,
                ["AddRoundedHighNarrowingUpper.Vector128.SByte"] = AddRoundedHighNarrowingUpper_Vector128_SByte,
                ["AddRoundedHighNarrowingUpper.Vector128.UInt16"] = AddRoundedHighNarrowingUpper_Vector128_UInt16,
                ["AddRoundedHighNarrowingUpper.Vector128.UInt32"] = AddRoundedHighNarrowingUpper_Vector128_UInt32,
            };
        }
    }
}
