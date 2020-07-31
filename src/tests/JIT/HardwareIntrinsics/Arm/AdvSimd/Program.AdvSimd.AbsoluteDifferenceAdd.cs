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
                ["AbsoluteDifferenceAdd.Vector64.Byte"] = AbsoluteDifferenceAdd_Vector64_Byte,
                ["AbsoluteDifferenceAdd.Vector64.Int16"] = AbsoluteDifferenceAdd_Vector64_Int16,
                ["AbsoluteDifferenceAdd.Vector64.Int32"] = AbsoluteDifferenceAdd_Vector64_Int32,
                ["AbsoluteDifferenceAdd.Vector64.SByte"] = AbsoluteDifferenceAdd_Vector64_SByte,
                ["AbsoluteDifferenceAdd.Vector64.UInt16"] = AbsoluteDifferenceAdd_Vector64_UInt16,
                ["AbsoluteDifferenceAdd.Vector64.UInt32"] = AbsoluteDifferenceAdd_Vector64_UInt32,
                ["AbsoluteDifferenceAdd.Vector128.Byte"] = AbsoluteDifferenceAdd_Vector128_Byte,
                ["AbsoluteDifferenceAdd.Vector128.Int16"] = AbsoluteDifferenceAdd_Vector128_Int16,
                ["AbsoluteDifferenceAdd.Vector128.Int32"] = AbsoluteDifferenceAdd_Vector128_Int32,
                ["AbsoluteDifferenceAdd.Vector128.SByte"] = AbsoluteDifferenceAdd_Vector128_SByte,
                ["AbsoluteDifferenceAdd.Vector128.UInt16"] = AbsoluteDifferenceAdd_Vector128_UInt16,
                ["AbsoluteDifferenceAdd.Vector128.UInt32"] = AbsoluteDifferenceAdd_Vector128_UInt32,
            };
        }
    }
}
