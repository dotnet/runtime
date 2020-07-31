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
                ["AbsoluteDifferenceWideningUpper.Vector128.Byte"] = AbsoluteDifferenceWideningUpper_Vector128_Byte,
                ["AbsoluteDifferenceWideningUpper.Vector128.Int16"] = AbsoluteDifferenceWideningUpper_Vector128_Int16,
                ["AbsoluteDifferenceWideningUpper.Vector128.Int32"] = AbsoluteDifferenceWideningUpper_Vector128_Int32,
                ["AbsoluteDifferenceWideningUpper.Vector128.SByte"] = AbsoluteDifferenceWideningUpper_Vector128_SByte,
                ["AbsoluteDifferenceWideningUpper.Vector128.UInt16"] = AbsoluteDifferenceWideningUpper_Vector128_UInt16,
                ["AbsoluteDifferenceWideningUpper.Vector128.UInt32"] = AbsoluteDifferenceWideningUpper_Vector128_UInt32,
            };
        }
    }
}
