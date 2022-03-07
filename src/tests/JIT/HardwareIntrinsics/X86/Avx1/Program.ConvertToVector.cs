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
                ["ConvertToVector128Int32.Double"] = ConvertToVector128Int32Double,
                ["ConvertToVector128Single.Double"] = ConvertToVector128SingleDouble,
                ["ConvertToVector256Int32.Single"] = ConvertToVector256Int32Single,
                ["ConvertToVector256Single.Int32"] = ConvertToVector256SingleInt32,
                ["ConvertToVector256Double.Single"] = ConvertToVector256DoubleSingle,
                ["ConvertToVector256Double.Int32"] = ConvertToVector256DoubleInt32,
                ["ConvertToVector128Int32WithTruncation.Double"] = ConvertToVector128Int32WithTruncationDouble,
                ["ConvertToVector256Int32WithTruncation.Single"] = ConvertToVector256Int32WithTruncationSingle,
            };
        }
    }
}
