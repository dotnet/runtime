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
                ["ConvertToVector128Int16.Byte"] = ConvertToVector128Int16Byte,
                ["ConvertToVector128Int16.SByte"] = ConvertToVector128Int16SByte,
                ["ConvertToVector128Int32.Byte"] = ConvertToVector128Int32Byte,
                ["ConvertToVector128Int32.Int16"] = ConvertToVector128Int32Int16,
                ["ConvertToVector128Int32.SByte"] = ConvertToVector128Int32SByte,
                ["ConvertToVector128Int32.UInt16"] = ConvertToVector128Int32UInt16,
                ["ConvertToVector128Int64.Byte"] = ConvertToVector128Int64Byte,
                ["ConvertToVector128Int64.Int16"] = ConvertToVector128Int64Int16,
                ["ConvertToVector128Int64.Int32"] = ConvertToVector128Int64Int32,
                ["ConvertToVector128Int64.SByte"] = ConvertToVector128Int64SByte,
                ["ConvertToVector128Int64.UInt16"] = ConvertToVector128Int64UInt16,
                ["ConvertToVector128Int64.UInt32"] = ConvertToVector128Int64UInt32,
            };
        }
    }
}
