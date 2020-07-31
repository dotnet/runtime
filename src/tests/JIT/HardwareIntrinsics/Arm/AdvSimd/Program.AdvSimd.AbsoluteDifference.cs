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
                ["AbsoluteDifference.Vector64.Byte"] = AbsoluteDifference_Vector64_Byte,
                ["AbsoluteDifference.Vector64.Int16"] = AbsoluteDifference_Vector64_Int16,
                ["AbsoluteDifference.Vector64.Int32"] = AbsoluteDifference_Vector64_Int32,
                ["AbsoluteDifference.Vector64.SByte"] = AbsoluteDifference_Vector64_SByte,
                ["AbsoluteDifference.Vector64.Single"] = AbsoluteDifference_Vector64_Single,
                ["AbsoluteDifference.Vector64.UInt16"] = AbsoluteDifference_Vector64_UInt16,
                ["AbsoluteDifference.Vector64.UInt32"] = AbsoluteDifference_Vector64_UInt32,
                ["AbsoluteDifference.Vector128.Byte"] = AbsoluteDifference_Vector128_Byte,
                ["AbsoluteDifference.Vector128.Int16"] = AbsoluteDifference_Vector128_Int16,
                ["AbsoluteDifference.Vector128.Int32"] = AbsoluteDifference_Vector128_Int32,
                ["AbsoluteDifference.Vector128.SByte"] = AbsoluteDifference_Vector128_SByte,
                ["AbsoluteDifference.Vector128.Single"] = AbsoluteDifference_Vector128_Single,
                ["AbsoluteDifference.Vector128.UInt16"] = AbsoluteDifference_Vector128_UInt16,
                ["AbsoluteDifference.Vector128.UInt32"] = AbsoluteDifference_Vector128_UInt32,
            };
        }
    }
}
