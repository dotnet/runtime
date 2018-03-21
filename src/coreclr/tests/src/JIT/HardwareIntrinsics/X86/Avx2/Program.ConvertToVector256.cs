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
                ["ConvertToVector256UInt16.Byte"] = ConvertToVector256UInt16Byte,
                ["ConvertToVector256Int16.SByte"] = ConvertToVector256Int16SByte,
                ["ConvertToVector256UInt32.Byte"] = ConvertToVector256UInt32Byte,
                ["ConvertToVector256Int32.Int16"] = ConvertToVector256Int32Int16,
                ["ConvertToVector256Int32.SByte"] = ConvertToVector256Int32SByte,
                ["ConvertToVector256UInt32.UInt16"] = ConvertToVector256UInt32UInt16,
                ["ConvertToVector256UInt64.Byte"] = ConvertToVector256UInt64Byte,
                ["ConvertToVector256Int64.Int16"] = ConvertToVector256Int64Int16,
                ["ConvertToVector256Int64.Int32"] = ConvertToVector256Int64Int32,
                ["ConvertToVector256Int64.SByte"] = ConvertToVector256Int64SByte,
                ["ConvertToVector256UInt64.UInt16"] = ConvertToVector256UInt64UInt16,
                ["ConvertToVector256UInt64.UInt32"] = ConvertToVector256UInt64UInt32,
            };
        }
    }
}
