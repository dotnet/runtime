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
                ["MaskLoad.Double"] = MaskLoadDouble,
                ["MaskLoad.Single"] = MaskLoadSingle,
                ["MaskStore.Double"] = MaskStoreDouble,
                ["MaskStore.Single"] = MaskStoreSingle,
                ["PermuteVar.Single"] = PermuteVarSingle,
                ["PermuteVar.Double"] = PermuteVarDouble,
            };
        }
    }
}
