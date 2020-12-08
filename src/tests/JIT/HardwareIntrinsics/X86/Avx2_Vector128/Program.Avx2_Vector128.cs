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
                ["ShiftRightArithmeticVariable.Int32"] = ShiftRightArithmeticVariableInt32,
                ["BroadcastScalarToVector128.Byte"] = BroadcastScalarToVector128Byte,
                ["BroadcastScalarToVector128.SByte"] = BroadcastScalarToVector128SByte,
                ["BroadcastScalarToVector128.Int16"] = BroadcastScalarToVector128Int16,
                ["BroadcastScalarToVector128.UInt16"] = BroadcastScalarToVector128UInt16,
                ["BroadcastScalarToVector128.Int32"] = BroadcastScalarToVector128Int32,
                ["BroadcastScalarToVector128.UInt32"] = BroadcastScalarToVector128UInt32,
                ["BroadcastScalarToVector128.Int64"] = BroadcastScalarToVector128Int64,
                ["BroadcastScalarToVector128.UInt64"] = BroadcastScalarToVector128UInt64,
                ["BroadcastScalarToVector256.Byte"] = BroadcastScalarToVector256Byte,
                ["BroadcastScalarToVector256.SByte"] = BroadcastScalarToVector256SByte,
                ["BroadcastScalarToVector256.Int16"] = BroadcastScalarToVector256Int16,
                ["BroadcastScalarToVector256.UInt16"] = BroadcastScalarToVector256UInt16,
                ["BroadcastScalarToVector256.Int32"] = BroadcastScalarToVector256Int32,
                ["BroadcastScalarToVector256.UInt32"] = BroadcastScalarToVector256UInt32,
                ["BroadcastScalarToVector256.Int64"] = BroadcastScalarToVector256Int64,
                ["BroadcastScalarToVector256.UInt64"] = BroadcastScalarToVector256UInt64,
            };
        }
    }
}
