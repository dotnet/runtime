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
                ["AddAcrossWidening.Vector64.Byte"] = AddAcrossWidening_Vector64_Byte,
                ["AddAcrossWidening.Vector64.Int16"] = AddAcrossWidening_Vector64_Int16,
                ["AddAcrossWidening.Vector64.SByte"] = AddAcrossWidening_Vector64_SByte,
                ["AddAcrossWidening.Vector64.UInt16"] = AddAcrossWidening_Vector64_UInt16,
                ["AddAcrossWidening.Vector128.Byte"] = AddAcrossWidening_Vector128_Byte,
                ["AddAcrossWidening.Vector128.Int16"] = AddAcrossWidening_Vector128_Int16,
                ["AddAcrossWidening.Vector128.Int32"] = AddAcrossWidening_Vector128_Int32,
                ["AddAcrossWidening.Vector128.SByte"] = AddAcrossWidening_Vector128_SByte,
                ["AddAcrossWidening.Vector128.UInt16"] = AddAcrossWidening_Vector128_UInt16,
                ["AddAcrossWidening.Vector128.UInt32"] = AddAcrossWidening_Vector128_UInt32,
            };
        }
    }
}
