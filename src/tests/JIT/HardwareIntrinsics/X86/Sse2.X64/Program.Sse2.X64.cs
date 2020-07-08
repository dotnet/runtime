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
                ["ConvertToInt64.Vector128Double"] = ConvertToInt64Vector128Double,
                ["ConvertToInt64.Vector128Int64"] = ConvertToInt64Vector128Int64,
                ["ConvertToUInt64.Vector128UInt64"] = ConvertToUInt64Vector128UInt64,
                ["ConvertToInt64WithTruncation.Vector128Double"] = ConvertToInt64WithTruncationVector128Double,
                ["ConvertScalarToVector128Double.Double"] = ConvertScalarToVector128DoubleDouble,
                ["ConvertScalarToVector128Int64.Int64"] = ConvertScalarToVector128Int64Int64,
                ["ConvertScalarToVector128UInt64.UInt64"] = ConvertScalarToVector128UInt64UInt64,
            };
        }
    }
}
