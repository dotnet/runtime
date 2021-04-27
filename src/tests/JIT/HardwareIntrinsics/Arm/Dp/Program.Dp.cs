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
            TestList = new Dictionary<string, Action>() {
                ["DotProduct.Vector64.Int32"] = DotProduct_Vector64_Int32,
                ["DotProduct.Vector64.UInt32"] = DotProduct_Vector64_UInt32,
                ["DotProduct.Vector128.Int32"] = DotProduct_Vector128_Int32,
                ["DotProduct.Vector128.UInt32"] = DotProduct_Vector128_UInt32,
                ["DotProductBySelectedQuadruplet.Vector64.Int32.Vector64.SByte.1"] = DotProductBySelectedQuadruplet_Vector64_Int32_Vector64_SByte_1,
                ["DotProductBySelectedQuadruplet.Vector64.Int32.Vector128.SByte.3"] = DotProductBySelectedQuadruplet_Vector64_Int32_Vector128_SByte_3,
                ["DotProductBySelectedQuadruplet.Vector64.UInt32.Vector64.Byte.1"] = DotProductBySelectedQuadruplet_Vector64_UInt32_Vector64_Byte_1,
                ["DotProductBySelectedQuadruplet.Vector64.UInt32.Vector128.Byte.3"] = DotProductBySelectedQuadruplet_Vector64_UInt32_Vector128_Byte_3,
                ["DotProductBySelectedQuadruplet.Vector128.Int32.Vector64.SByte.1"] = DotProductBySelectedQuadruplet_Vector128_Int32_Vector64_SByte_1,
                ["DotProductBySelectedQuadruplet.Vector128.Int32.Vector128.SByte.3"] = DotProductBySelectedQuadruplet_Vector128_Int32_Vector128_SByte_3,
                ["DotProductBySelectedQuadruplet.Vector128.UInt32.Vector64.Byte.1"] = DotProductBySelectedQuadruplet_Vector128_UInt32_Vector64_Byte_1,
                ["DotProductBySelectedQuadruplet.Vector128.UInt32.Vector128.Byte.3"] = DotProductBySelectedQuadruplet_Vector128_UInt32_Vector128_Byte_3,
            };
        }
    }
}
