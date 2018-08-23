// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Blend.Int32.1"] = BlendInt321,
                ["Blend.Int32.2"] = BlendInt322,
                ["Blend.Int32.4"] = BlendInt324,
                ["Blend.Int32.85"] = BlendInt3285,
                ["Blend.UInt32.1"] = BlendUInt321,
                ["Blend.UInt32.2"] = BlendUInt322,
                ["Blend.UInt32.4"] = BlendUInt324,
                ["Blend.UInt32.85"] = BlendUInt3285,
                ["MaskLoad.Int32"] = MaskLoadInt32,
                ["MaskLoad.UInt32"] = MaskLoadUInt32,
                ["MaskLoad.Int64"] = MaskLoadInt64,
                ["MaskLoad.UInt64"] = MaskLoadUInt64,
                ["MaskStore.Int32"] = MaskStoreInt32,
                ["MaskStore.UInt32"] = MaskStoreUInt32,
                ["MaskStore.Int64"] = MaskStoreInt64,
                ["MaskStore.UInt64"] = MaskStoreUInt64,
            };
        }
    }
}
