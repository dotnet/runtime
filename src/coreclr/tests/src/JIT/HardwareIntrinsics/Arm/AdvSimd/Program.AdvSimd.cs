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
                ["Abs.Vector64.Byte"] = Abs_Vector64_Byte,
                ["Abs.Vector64.UInt16"] = Abs_Vector64_UInt16,
                ["Abs.Vector64.UInt32"] = Abs_Vector64_UInt32,
                ["Abs.Vector64.Single"] = Abs_Vector64_Single,
                ["Abs.Vector128.Byte"] = Abs_Vector128_Byte,
                ["Abs.Vector128.Single"] = Abs_Vector128_Single,
                ["Abs.Vector128.UInt16"] = Abs_Vector128_UInt16,
                ["Abs.Vector128.UInt32"] = Abs_Vector128_UInt32,
                ["AbsScalar.Vector64.Single"] = AbsScalar_Vector64_Single,
                ["Add.Vector64.Byte"] = Add_Vector64_Byte,
                ["Add.Vector64.Int16"] = Add_Vector64_Int16,
                ["Add.Vector64.Int32"] = Add_Vector64_Int32,
                ["Add.Vector64.SByte"] = Add_Vector64_SByte,
                ["Add.Vector64.Single"] = Add_Vector64_Single,
                ["Add.Vector64.UInt16"] = Add_Vector64_UInt16,
                ["Add.Vector64.UInt32"] = Add_Vector64_UInt32,
                ["Add.Vector128.Byte"] = Add_Vector128_Byte,
                ["Add.Vector128.Int16"] = Add_Vector128_Int16,
                ["Add.Vector128.Int32"] = Add_Vector128_Int32,
                ["Add.Vector128.Int64"] = Add_Vector128_Int64,
                ["Add.Vector128.SByte"] = Add_Vector128_SByte,
                ["Add.Vector128.Single"] = Add_Vector128_Single,
                ["Add.Vector128.UInt16"] = Add_Vector128_UInt16,
                ["Add.Vector128.UInt32"] = Add_Vector128_UInt32,
                ["Add.Vector128.UInt64"] = Add_Vector128_UInt64,
                ["AddScalar.Vector64.Single"] = AddScalar_Vector64_Single,
                ["LoadVector64.Byte"] = LoadVector64_Byte,
                ["LoadVector64.Int16"] = LoadVector64_Int16,
                ["LoadVector64.Int32"] = LoadVector64_Int32,
                ["LoadVector64.SByte"] = LoadVector64_SByte,
                ["LoadVector64.Single"] = LoadVector64_Single,
                ["LoadVector64.UInt16"] = LoadVector64_UInt16,
                ["LoadVector64.UInt32"] = LoadVector64_UInt32,
                ["LoadVector128.Byte"] = LoadVector128_Byte,
                ["LoadVector128.Double"] = LoadVector128_Double,
                ["LoadVector128.Int16"] = LoadVector128_Int16,
                ["LoadVector128.Int32"] = LoadVector128_Int32,
                ["LoadVector128.Int64"] = LoadVector128_Int64,
                ["LoadVector128.SByte"] = LoadVector128_SByte,
                ["LoadVector128.Single"] = LoadVector128_Single,
                ["LoadVector128.UInt16"] = LoadVector128_UInt16,
                ["LoadVector128.UInt32"] = LoadVector128_UInt32,
                ["LoadVector128.UInt64"] = LoadVector128_UInt64,
            };
        }
    }
}
