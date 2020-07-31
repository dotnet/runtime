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
                ["AbsoluteDifferenceWideningLowerAndAdd.Vector64.Byte"] = AbsoluteDifferenceWideningLowerAndAdd_Vector64_Byte,
                ["AbsoluteDifferenceWideningLowerAndAdd.Vector64.Int16"] = AbsoluteDifferenceWideningLowerAndAdd_Vector64_Int16,
                ["AbsoluteDifferenceWideningLowerAndAdd.Vector64.Int32"] = AbsoluteDifferenceWideningLowerAndAdd_Vector64_Int32,
                ["AbsoluteDifferenceWideningLowerAndAdd.Vector64.SByte"] = AbsoluteDifferenceWideningLowerAndAdd_Vector64_SByte,
                ["AbsoluteDifferenceWideningLowerAndAdd.Vector64.UInt16"] = AbsoluteDifferenceWideningLowerAndAdd_Vector64_UInt16,
                ["AbsoluteDifferenceWideningLowerAndAdd.Vector64.UInt32"] = AbsoluteDifferenceWideningLowerAndAdd_Vector64_UInt32,
            };
        }
    }
}
