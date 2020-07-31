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
                ["SignExtendWideningUpper.Vector128.Int16"] = SignExtendWideningUpper_Vector128_Int16,
                ["SignExtendWideningUpper.Vector128.Int32"] = SignExtendWideningUpper_Vector128_Int32,
                ["SignExtendWideningUpper.Vector128.SByte"] = SignExtendWideningUpper_Vector128_SByte,
            };
        }
    }
}
