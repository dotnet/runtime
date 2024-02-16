// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    internal static class GCStaticRegionConstants
    {
        /// <summary>
        /// Flag set if the corresponding GCStatic entry has not yet been initialized and
        /// the corresponding MethodTable pointer has been changed into a instance pointer of
        /// that MethodTable.
        /// </summary>
        public const int Uninitialized = 0x1;

        /// <summary>
        /// Flag set if the next pointer loc points to GCStaticsPreInitDataNode.
        /// Otherise it is the next GCStatic entry.
        /// </summary>
        public const int HasPreInitializedData = 0x2;

        public const int Mask = Uninitialized | HasPreInitializedData;
    }

    internal static class ArrayTypesConstants
    {
        /// <summary>
        /// Maximum allowable size for array element types.
        /// </summary>
        public const int MaxSizeForValueClassInArray = 0xFFFF;
    }

    // keep in sync with GC_ALLOC_FLAGS in gcinterface.h
    internal enum GC_ALLOC_FLAGS
    {
        GC_ALLOC_NO_FLAGS = 0,
        GC_ALLOC_ZEROING_OPTIONAL = 16,
        GC_ALLOC_PINNED_OBJECT_HEAP = 64,
    }

    internal static class SpecialDispatchMapSlot
    {
        public const ushort Diamond = 0xFFFE;
        public const ushort Reabstraction = 0xFFFF;
    }

    internal static class SpecialGVMInterfaceEntry
    {
        public const uint Diamond = 0xFFFFFFFF;
        public const uint Reabstraction = 0xFFFFFFFE;
    }

    internal static class StaticVirtualMethodContextSource
    {
        public const ushort None = 0;
        public const ushort ContextFromThisClass = 1;
        public const ushort ContextFromFirstInterface = 2;
    }

    internal enum RuntimeHelperKind
    {
        AllocateObject,
        IsInst,
        CastClass,
        AllocateArray,
    }

    /// <summary>
    /// Constants that describe the bits of the Flags field of MethodFixupCell.
    /// </summary>
    internal static class MethodFixupCellFlagsConstants
    {
        public const int CharSetMask = 0x7;
        public const int IsObjectiveCMessageSendMask = 0x8;
        public const int ObjectiveCMessageSendFunctionMask = 0x70;
        public const int ObjectiveCMessageSendFunctionShift = 4;
    }
}
