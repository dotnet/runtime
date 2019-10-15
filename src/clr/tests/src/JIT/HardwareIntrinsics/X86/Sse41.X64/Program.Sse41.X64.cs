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
                ["Extract.Int64.129"] = ExtractInt64129,
                ["Extract.UInt64.129"] = ExtractUInt64129,
                ["Extract.Int64.1"] = ExtractInt641,
                ["Extract.UInt64.1"] = ExtractUInt641,
                ["Insert.Int64.129"] = InsertInt64129,
                ["Insert.UInt64.129"] = InsertUInt64129,
                ["Insert.Int64.1"] = InsertInt641,
                ["Insert.UInt64.1"] = InsertUInt641,
            };
        }
    }
}
