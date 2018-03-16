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
                ["ExtractVector128.Byte.1"] = ExtractVector128Byte1,
                ["ExtractVector128.SByte.1"] = ExtractVector128SByte1,
                ["ExtractVector128.Int16.1"] = ExtractVector128Int161,
                ["ExtractVector128.UInt16.1"] = ExtractVector128UInt161,
                ["ExtractVector128.Int32.1"] = ExtractVector128Int321,
                ["ExtractVector128.UInt32.1"] = ExtractVector128UInt321,
                ["ExtractVector128.Int64.1"] = ExtractVector128Int641,
                ["ExtractVector128.UInt64.1"] = ExtractVector128UInt641,
                ["ExtractVector128.Single.1"] = ExtractVector128Single1,
                ["ExtractVector128.Double.1"] = ExtractVector128Double1,
                ["InsertVector128.Byte.1"] = InsertVector128Byte1,
                ["InsertVector128.SByte.1"] = InsertVector128SByte1,
                ["InsertVector128.Int16.1"] = InsertVector128Int161,
                ["InsertVector128.UInt16.1"] = InsertVector128UInt161,
                ["InsertVector128.Int32.1"] = InsertVector128Int321,
                ["InsertVector128.UInt32.1"] = InsertVector128UInt321,
                ["InsertVector128.Int64.1"] = InsertVector128Int641,
                ["InsertVector128.UInt64.1"] = InsertVector128UInt641,
                ["InsertVector128.Single.1"] = InsertVector128Single1,
                ["InsertVector128.Double.1"] = InsertVector128Double1,
            };
        }
    }
}
