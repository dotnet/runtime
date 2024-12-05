// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

// Non-vtable slot, native code slot, and MethodImpl slots are stored after the MethodDesc itself, packed tightly
// in the order: [non-vtable; method impl; native code].
internal static class MethodDescOptionalSlots
{
    internal static bool HasNonVtableSlot(ushort flags)
        => (flags & (ushort)MethodDescFlags_1.MethodDescFlags.HasNonVtableSlot) != 0;

    internal static bool HasMethodImpl(ushort flags)
        => (flags & (ushort)MethodDescFlags_1.MethodDescFlags.HasMethodImpl) != 0;

    internal static bool HasNativeCodeSlot(ushort flags)
        => (flags & (ushort)MethodDescFlags_1.MethodDescFlags.HasNativeCodeSlot) != 0;

    // Offsets are from the start of optional slots data (so right after the MethodDesc)
    internal static uint NonVtableSlotOffset(ushort flags)
    {
        if (!HasNonVtableSlot(flags))
            throw new InvalidOperationException("no non-vtable slot");

        return 0u;
    }

    internal static uint MethodImplOffset(ushort flags, Target target)
    {
        if (!HasMethodImpl(flags))
            throw new InvalidOperationException("no method impl slot");

        return HasNonVtableSlot(flags) ? (uint)target.PointerSize : 0;
    }

    internal static uint NativeCodeSlotOffset(ushort flags, Target target)
    {
        if (!HasNativeCodeSlot(flags))
            throw new InvalidOperationException("no native code slot");

        uint offset = 0;
        if (HasNonVtableSlot(flags))
            offset += (uint)target.PointerSize;

        if (HasMethodImpl(flags))
            offset += target.GetTypeInfo(DataType.MethodImpl).Size!.Value;

        return offset;
    }
}
