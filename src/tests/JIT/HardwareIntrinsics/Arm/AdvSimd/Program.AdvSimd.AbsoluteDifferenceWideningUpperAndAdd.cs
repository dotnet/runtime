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
                ["AbsoluteDifferenceWideningUpperAndAdd.Vector128.Byte"] = AbsoluteDifferenceWideningUpperAndAdd_Vector128_Byte,
                ["AbsoluteDifferenceWideningUpperAndAdd.Vector128.Int16"] = AbsoluteDifferenceWideningUpperAndAdd_Vector128_Int16,
                ["AbsoluteDifferenceWideningUpperAndAdd.Vector128.Int32"] = AbsoluteDifferenceWideningUpperAndAdd_Vector128_Int32,
                ["AbsoluteDifferenceWideningUpperAndAdd.Vector128.SByte"] = AbsoluteDifferenceWideningUpperAndAdd_Vector128_SByte,
                ["AbsoluteDifferenceWideningUpperAndAdd.Vector128.UInt16"] = AbsoluteDifferenceWideningUpperAndAdd_Vector128_UInt16,
                ["AbsoluteDifferenceWideningUpperAndAdd.Vector128.UInt32"] = AbsoluteDifferenceWideningUpperAndAdd_Vector128_UInt32,
            };
        }
    }
}
