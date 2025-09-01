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

    internal static TargetPointer GetAddressOfNonVtableSlot(TargetPointer methodDesc, MethodClassification classification, ushort flags, Target target)
    {
        uint offset = StartOffset(classification, target);
        offset += NonVtableSlotOffset(flags);
        return methodDesc + offset;
    }

    internal static TargetPointer GetAddressOfNativeCodeSlot(TargetPointer methodDesc, MethodClassification classification, ushort flags, Target target)
    {
        uint offset = StartOffset(classification, target);
        offset += NativeCodeSlotOffset(flags, target);
        return methodDesc + offset;
    }

    // Offset from the MethodDesc address to the start of its optional slots
    private static uint StartOffset(MethodClassification classification, Target target)
    {
        // See MethodDesc::GetBaseSize and s_ClassificationSizeTable
        // sizeof(MethodDesc),                 mcIL
        // sizeof(FCallMethodDesc),            mcFCall
        // sizeof(PInvokeMethodDesc),          mcPInvoke
        // sizeof(EEImplMethodDesc),           mcEEImpl
        // sizeof(ArrayMethodDesc),            mcArray
        // sizeof(InstantiatedMethodDesc),     mcInstantiated
        // sizeof(CLRToCOMCallMethodDesc),     mcComInterOp
        // sizeof(DynamicMethodDesc)           mcDynamic
        DataType type = classification switch
        {
            MethodClassification.IL => DataType.MethodDesc,
            MethodClassification.FCall => DataType.FCallMethodDesc,
            MethodClassification.PInvoke => DataType.PInvokeMethodDesc,
            MethodClassification.EEImpl => DataType.EEImplMethodDesc,
            MethodClassification.Array => DataType.ArrayMethodDesc,
            MethodClassification.Instantiated => DataType.InstantiatedMethodDesc,
            MethodClassification.ComInterop => DataType.CLRToCOMCallMethodDesc,
            MethodClassification.Dynamic => DataType.DynamicMethodDesc,
            _ => throw new InvalidOperationException($"Unexpected method classification 0x{classification:x2} for MethodDesc")
        };
        return target.GetTypeInfo(type).Size ?? throw new InvalidOperationException($"size of MethodDesc not known");
    }

    // Offsets are from the start of optional slots data (so right after the MethodDesc), obtained via StartOffset
    private static uint NonVtableSlotOffset(ushort flags)
    {
        if (!HasNonVtableSlot(flags))
            throw new InvalidOperationException("no non-vtable slot");

        return 0u;
    }

    private static uint MethodImplOffset(ushort flags, Target target)
    {
        if (!HasMethodImpl(flags))
            throw new InvalidOperationException("no method impl slot");

        return HasNonVtableSlot(flags) ? (uint)target.PointerSize : 0;
    }

    private static uint NativeCodeSlotOffset(ushort flags, Target target)
    {
        if (!HasNativeCodeSlot(flags))
            throw new InvalidOperationException("no native code slot");

        uint offset = 0;
        if (HasNonVtableSlot(flags))
            offset += target.GetTypeInfo(DataType.NonVtableSlot).Size!.Value;

        if (HasMethodImpl(flags))
            offset += target.GetTypeInfo(DataType.MethodImpl).Size!.Value;

        return offset;
    }
}
