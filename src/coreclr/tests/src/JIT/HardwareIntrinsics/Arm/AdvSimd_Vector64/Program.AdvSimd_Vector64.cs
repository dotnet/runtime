// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Abs.Byte"] = AbsByte,
                ["Abs.UInt16"] = AbsUInt16,
                ["Abs.UInt32"] = AbsUInt32,
                ["Abs.Single"] = AbsSingle,
                ["AbsScalar.Single"] = AbsScalarSingle,
                ["Add.Byte"] = AddByte,
                ["Add.Int16"] = AddInt16,
                ["Add.Int32"] = AddInt32,
                ["Add.SByte"] = AddSByte,
                ["Add.Single"] = AddSingle,
                ["Add.UInt16"] = AddUInt16,
                ["Add.UInt32"] = AddUInt32,
                ["AddScalar.Single"] = AddScalarSingle,
            };
        }
    }
}
