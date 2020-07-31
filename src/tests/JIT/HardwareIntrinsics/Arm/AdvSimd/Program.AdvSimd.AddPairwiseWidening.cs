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
                ["AddPairwiseWidening.Vector64.Byte"] = AddPairwiseWidening_Vector64_Byte,
                ["AddPairwiseWidening.Vector64.Int16"] = AddPairwiseWidening_Vector64_Int16,
                ["AddPairwiseWidening.Vector64.SByte"] = AddPairwiseWidening_Vector64_SByte,
                ["AddPairwiseWidening.Vector64.UInt16"] = AddPairwiseWidening_Vector64_UInt16,
                ["AddPairwiseWidening.Vector128.Byte"] = AddPairwiseWidening_Vector128_Byte,
                ["AddPairwiseWidening.Vector128.Int16"] = AddPairwiseWidening_Vector128_Int16,
                ["AddPairwiseWidening.Vector128.Int32"] = AddPairwiseWidening_Vector128_Int32,
                ["AddPairwiseWidening.Vector128.SByte"] = AddPairwiseWidening_Vector128_SByte,
                ["AddPairwiseWidening.Vector128.UInt16"] = AddPairwiseWidening_Vector128_UInt16,
                ["AddPairwiseWidening.Vector128.UInt32"] = AddPairwiseWidening_Vector128_UInt32,
            };
        }
    }
}
