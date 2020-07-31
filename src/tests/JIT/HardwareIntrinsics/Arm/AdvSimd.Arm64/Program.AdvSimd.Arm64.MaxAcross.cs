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
                ["MaxAcross.Vector64.Byte"] = MaxAcross_Vector64_Byte,
                ["MaxAcross.Vector64.Int16"] = MaxAcross_Vector64_Int16,
                ["MaxAcross.Vector64.SByte"] = MaxAcross_Vector64_SByte,
                ["MaxAcross.Vector64.UInt16"] = MaxAcross_Vector64_UInt16,
                ["MaxAcross.Vector128.Byte"] = MaxAcross_Vector128_Byte,
                ["MaxAcross.Vector128.Int16"] = MaxAcross_Vector128_Int16,
                ["MaxAcross.Vector128.Int32"] = MaxAcross_Vector128_Int32,
                ["MaxAcross.Vector128.SByte"] = MaxAcross_Vector128_SByte,
                ["MaxAcross.Vector128.Single"] = MaxAcross_Vector128_Single,
                ["MaxAcross.Vector128.UInt16"] = MaxAcross_Vector128_UInt16,
                ["MaxAcross.Vector128.UInt32"] = MaxAcross_Vector128_UInt32,
            };
        }
    }
}
