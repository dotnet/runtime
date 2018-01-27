// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Add.Byte"] = AddByte,
                ["Add.Int16"] = AddInt16,
                ["Add.Int32"] = AddInt32,
                ["Add.Int64"] = AddInt64,
                ["Add.SByte"] = AddSByte,
                ["Add.UInt16"] = AddUInt16,
                ["Add.UInt32"] = AddUInt32,
                ["Add.UInt64"] = AddUInt64,
            };
        }
    }
}
