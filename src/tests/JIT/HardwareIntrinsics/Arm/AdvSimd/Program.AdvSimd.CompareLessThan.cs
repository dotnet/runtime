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
                ["CompareLessThan.Vector64.Byte"] = CompareLessThan_Vector64_Byte,
                ["CompareLessThan.Vector64.Int16"] = CompareLessThan_Vector64_Int16,
                ["CompareLessThan.Vector64.Int32"] = CompareLessThan_Vector64_Int32,
                ["CompareLessThan.Vector64.SByte"] = CompareLessThan_Vector64_SByte,
                ["CompareLessThan.Vector64.Single"] = CompareLessThan_Vector64_Single,
                ["CompareLessThan.Vector64.UInt16"] = CompareLessThan_Vector64_UInt16,
                ["CompareLessThan.Vector64.UInt32"] = CompareLessThan_Vector64_UInt32,
                ["CompareLessThan.Vector128.Byte"] = CompareLessThan_Vector128_Byte,
                ["CompareLessThan.Vector128.Int16"] = CompareLessThan_Vector128_Int16,
                ["CompareLessThan.Vector128.Int32"] = CompareLessThan_Vector128_Int32,
                ["CompareLessThan.Vector128.SByte"] = CompareLessThan_Vector128_SByte,
                ["CompareLessThan.Vector128.Single"] = CompareLessThan_Vector128_Single,
                ["CompareLessThan.Vector128.UInt16"] = CompareLessThan_Vector128_UInt16,
                ["CompareLessThan.Vector128.UInt32"] = CompareLessThan_Vector128_UInt32,
            };
        }
    }
}
