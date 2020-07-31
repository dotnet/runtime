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
                ["AddAcross.Vector64.Byte"] = AddAcross_Vector64_Byte,
                ["AddAcross.Vector64.Int16"] = AddAcross_Vector64_Int16,
                ["AddAcross.Vector64.SByte"] = AddAcross_Vector64_SByte,
                ["AddAcross.Vector64.UInt16"] = AddAcross_Vector64_UInt16,
                ["AddAcross.Vector128.Byte"] = AddAcross_Vector128_Byte,
                ["AddAcross.Vector128.Int16"] = AddAcross_Vector128_Int16,
                ["AddAcross.Vector128.Int32"] = AddAcross_Vector128_Int32,
                ["AddAcross.Vector128.SByte"] = AddAcross_Vector128_SByte,
                ["AddAcross.Vector128.UInt16"] = AddAcross_Vector128_UInt16,
                ["AddAcross.Vector128.UInt32"] = AddAcross_Vector128_UInt32,
            };
        }
    }
}
