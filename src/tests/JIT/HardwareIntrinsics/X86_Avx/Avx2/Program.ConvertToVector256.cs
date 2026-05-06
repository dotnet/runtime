// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["ConvertToVector256Int16.Byte"] = ConvertToVector256Int16Byte,
                ["ConvertToVector256Int16.SByte"] = ConvertToVector256Int16SByte,
                ["ConvertToVector256Int32.Byte"] = ConvertToVector256Int32Byte,
                ["ConvertToVector256Int32.Int16"] = ConvertToVector256Int32Int16,
                ["ConvertToVector256Int32.SByte"] = ConvertToVector256Int32SByte,
                ["ConvertToVector256Int32.UInt16"] = ConvertToVector256Int32UInt16,
                ["ConvertToVector256Int64.Byte"] = ConvertToVector256Int64Byte,
                ["ConvertToVector256Int64.Int16"] = ConvertToVector256Int64Int16,
                ["ConvertToVector256Int64.Int32"] = ConvertToVector256Int64Int32,
                ["ConvertToVector256Int64.SByte"] = ConvertToVector256Int64SByte,
                ["ConvertToVector256Int64.UInt16"] = ConvertToVector256Int64UInt16,
                ["ConvertToVector256Int64.UInt32"] = ConvertToVector256Int64UInt32,
            };
        }
    }
}
