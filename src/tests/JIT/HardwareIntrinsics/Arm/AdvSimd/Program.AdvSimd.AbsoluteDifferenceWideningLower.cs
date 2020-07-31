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
                ["AbsoluteDifferenceWideningLower.Vector64.Byte"] = AbsoluteDifferenceWideningLower_Vector64_Byte,
                ["AbsoluteDifferenceWideningLower.Vector64.Int16"] = AbsoluteDifferenceWideningLower_Vector64_Int16,
                ["AbsoluteDifferenceWideningLower.Vector64.Int32"] = AbsoluteDifferenceWideningLower_Vector64_Int32,
                ["AbsoluteDifferenceWideningLower.Vector64.SByte"] = AbsoluteDifferenceWideningLower_Vector64_SByte,
                ["AbsoluteDifferenceWideningLower.Vector64.UInt16"] = AbsoluteDifferenceWideningLower_Vector64_UInt16,
                ["AbsoluteDifferenceWideningLower.Vector64.UInt32"] = AbsoluteDifferenceWideningLower_Vector64_UInt32,
            };
        }
    }
}
