// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Abs.Vector128.Double"] = Abs_Vector128_Double,
                ["Abs.Vector128.UInt64"] = Abs_Vector128_UInt64,
                ["ReverseElementBits.Vector128.Byte"] = ReverseElementBits_Vector128_Byte,
                ["ReverseElementBits.Vector128.SByte"] = ReverseElementBits_Vector128_SByte,
                ["ReverseElementBits.Vector64.Byte"] = ReverseElementBits_Vector64_Byte,
                ["ReverseElementBits.Vector64.SByte"] = ReverseElementBits_Vector64_SByte,
                ["AddAcross.Vector64.Byte"] = AddAcross_Vector64_Byte,
                ["AddAcross.Vector64.SByte"] = AddAcross_Vector64_SByte,
                ["AddAcross.Vector64.UInt16"] = AddAcross_Vector64_UInt16,
                ["AddAcross.Vector64.Int16"] = AddAcross_Vector64_Int16,
                ["AddAcross.Vector128.Byte"] = AddAcross_Vector128_Byte,
                ["AddAcross.Vector128.SByte"] = AddAcross_Vector128_SByte,
                ["AddAcross.Vector128.UInt16"] = AddAcross_Vector128_UInt16,
                ["AddAcross.Vector128.Int16"] = AddAcross_Vector128_Int16,
                ["AddAcross.Vector128.UInt32"] = AddAcross_Vector128_UInt32,
                ["AddAcross.Vector128.Int32"] = AddAcross_Vector128_Int32,
            };
        }
    }
}
