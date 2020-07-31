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
                ["AddPairwiseWideningAndAdd.Vector64.Byte"] = AddPairwiseWideningAndAdd_Vector64_Byte,
                ["AddPairwiseWideningAndAdd.Vector64.Int16"] = AddPairwiseWideningAndAdd_Vector64_Int16,
                ["AddPairwiseWideningAndAdd.Vector64.SByte"] = AddPairwiseWideningAndAdd_Vector64_SByte,
                ["AddPairwiseWideningAndAdd.Vector64.UInt16"] = AddPairwiseWideningAndAdd_Vector64_UInt16,
                ["AddPairwiseWideningAndAdd.Vector128.Byte"] = AddPairwiseWideningAndAdd_Vector128_Byte,
                ["AddPairwiseWideningAndAdd.Vector128.Int16"] = AddPairwiseWideningAndAdd_Vector128_Int16,
                ["AddPairwiseWideningAndAdd.Vector128.Int32"] = AddPairwiseWideningAndAdd_Vector128_Int32,
                ["AddPairwiseWideningAndAdd.Vector128.SByte"] = AddPairwiseWideningAndAdd_Vector128_SByte,
                ["AddPairwiseWideningAndAdd.Vector128.UInt16"] = AddPairwiseWideningAndAdd_Vector128_UInt16,
                ["AddPairwiseWideningAndAdd.Vector128.UInt32"] = AddPairwiseWideningAndAdd_Vector128_UInt32,
            };
        }
    }
}
