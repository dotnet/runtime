// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.Metadata.Ecma335
{
    internal enum MetadataStreamKind
    {
        Illegal,
        Compressed,
        Uncompressed,
    }

    [Flags]
    internal enum TableMask : ulong
    {
        Module = 1UL << TableIndex.Module,
        TypeRef = 1UL << TableIndex.TypeRef,
        TypeDef = 1UL << TableIndex.TypeDef,
        FieldPtr = 1UL << TableIndex.FieldPtr,
        Field = 1UL << TableIndex.Field,
        MethodPtr = 1UL << TableIndex.MethodPtr,
        MethodDef = 1UL << TableIndex.MethodDef,
        ParamPtr = 1UL << TableIndex.ParamPtr,
        Param = 1UL << TableIndex.Param,
        InterfaceImpl = 1UL << TableIndex.InterfaceImpl,
        MemberRef = 1UL << TableIndex.MemberRef,
        Constant = 1UL << TableIndex.Constant,
        CustomAttribute = 1UL << TableIndex.CustomAttribute,
        FieldMarshal = 1UL << TableIndex.FieldMarshal,
        DeclSecurity = 1UL << TableIndex.DeclSecurity,
        ClassLayout = 1UL << TableIndex.ClassLayout,
        FieldLayout = 1UL << TableIndex.FieldLayout,
        StandAloneSig = 1UL << TableIndex.StandAloneSig,
        EventMap = 1UL << TableIndex.EventMap,
        EventPtr = 1UL << TableIndex.EventPtr,
        Event = 1UL << TableIndex.Event,
        PropertyMap = 1UL << TableIndex.PropertyMap,
        PropertyPtr = 1UL << TableIndex.PropertyPtr,
        Property = 1UL << TableIndex.Property,
        MethodSemantics = 1UL << TableIndex.MethodSemantics,
        MethodImpl = 1UL << TableIndex.MethodImpl,
        ModuleRef = 1UL << TableIndex.ModuleRef,
        TypeSpec = 1UL << TableIndex.TypeSpec,
        ImplMap = 1UL << TableIndex.ImplMap,
        FieldRva = 1UL << TableIndex.FieldRva,
        EnCLog = 1UL << TableIndex.EncLog,
        EnCMap = 1UL << TableIndex.EncMap,
        Assembly = 1UL << TableIndex.Assembly,
        // AssemblyProcessor = 1UL << TableIndices.AssemblyProcessor,
        // AssemblyOS = 1UL << TableIndices.AssemblyOS,
        AssemblyRef = 1UL << TableIndex.AssemblyRef,
        // AssemblyRefProcessor = 1UL << TableIndices.AssemblyRefProcessor,
        // AssemblyRefOS = 1UL << TableIndices.AssemblyRefOS,
        File = 1UL << TableIndex.File,
        ExportedType = 1UL << TableIndex.ExportedType,
        ManifestResource = 1UL << TableIndex.ManifestResource,
        NestedClass = 1UL << TableIndex.NestedClass,
        GenericParam = 1UL << TableIndex.GenericParam,
        MethodSpec = 1UL << TableIndex.MethodSpec,
        GenericParamConstraint = 1UL << TableIndex.GenericParamConstraint,

        PtrTables =
            FieldPtr
          | MethodPtr
          | ParamPtr
          | EventPtr
          | PropertyPtr,
        V2_0_TablesMask =
            Module
          | TypeRef
          | TypeDef
          | FieldPtr
          | Field
          | MethodPtr
          | MethodDef
          | ParamPtr
          | Param
          | InterfaceImpl
          | MemberRef
          | Constant
          | CustomAttribute
          | FieldMarshal
          | DeclSecurity
          | ClassLayout
          | FieldLayout
          | StandAloneSig
          | EventMap
          | EventPtr
          | Event
          | PropertyMap
          | PropertyPtr
          | Property
          | MethodSemantics
          | MethodImpl
          | ModuleRef
          | TypeSpec
          | ImplMap
          | FieldRva
          | EnCLog
          | EnCMap
          | Assembly
          | AssemblyRef
          | File
          | ExportedType
          | ManifestResource
          | NestedClass
          | GenericParam
          | MethodSpec
          | GenericParamConstraint,
    }

    internal enum HeapSizeFlag : byte
    {
        StringHeapLarge = 0x01, // 4 byte uint indexes used for string heap offsets
        GuidHeapLarge = 0x02,   // 4 byte uint indexes used for GUID heap offsets
        BlobHeapLarge = 0x04,   // 4 byte uint indexes used for Blob heap offsets
        EnCDeltas = 0x20,       // Indicates only EnC Deltas are present
        DeletedMarks = 0x80,    // Indicates metadata might contain items marked deleted
    }

    internal enum StringKind : byte
    {
        Plain = (byte)(StringHandleType.String >> HeapHandleType.OffsetBitCount),
        Virtual = (byte)(StringHandleType.VirtualString >> HeapHandleType.OffsetBitCount),
        WinRTPrefixed = (byte)(StringHandleType.WinRTPrefixedString >> HeapHandleType.OffsetBitCount),
        DotTerminated = (byte)(StringHandleType.DotTerminatedString >> HeapHandleType.OffsetBitCount),
    }

    internal static class StringHandleType
    {
        // NUL-terminated UTF8 string on a #String heap.
        internal const uint String = 0;

        // String on #String heap whose terminator is NUL and '.', whichever comes first.
        internal const uint DotTerminatedString = String | (1 << HeapHandleType.OffsetBitCount);

        // Reserved values that can be used for future strings:
        internal const uint ReservedString1 = String | (2 << HeapHandleType.OffsetBitCount);
        internal const uint ReservedString2 = String | (3 << HeapHandleType.OffsetBitCount);

        // Virtual string identified by a virtual index
        internal const uint VirtualString = HeapHandleType.VirtualBit | String;

        // Virtual string whose value is a "<WinRT>" prefixed string found at the specified heap offset.          
        internal const uint WinRTPrefixedString = HeapHandleType.VirtualBit | String | (1 << HeapHandleType.OffsetBitCount);

        // Reserved virtual strings that can be used in future:
        internal const uint ReservedVirtualString1 = HeapHandleType.VirtualBit | String | (2 << HeapHandleType.OffsetBitCount);
        internal const uint ReservedVirtualString2 = HeapHandleType.VirtualBit | String | (3 << HeapHandleType.OffsetBitCount);
    }

    internal static class HeapHandleType
    {
        // Heap offset values are limited to 29 bits (max compressed integer)
        internal const int OffsetBitCount = 29;
        internal const uint OffsetMask = (1 << OffsetBitCount) - 1;
        internal const uint VirtualBit = 0x80000000;
        internal const uint NonVirtualTypeMask = 3u << OffsetBitCount;
        internal const uint TypeMask = VirtualBit | NonVirtualTypeMask;

        internal static bool IsValidHeapOffset(uint offset)
        {
            return (offset & ~OffsetMask) == 0;
        }
    }

    internal static class HandleType
    {
        internal const uint Module = 0x00;
        internal const uint TypeRef = 0x01;
        internal const uint TypeDef = 0x02;
        internal const uint FieldDef = 0x04;
        internal const uint MethodDef = 0x06;
        internal const uint ParamDef = 0x08;
        internal const uint InterfaceImpl = 0x09;
        internal const uint MemberRef = 0x0a;
        internal const uint Constant = 0x0b;
        internal const uint CustomAttribute = 0x0c;
        internal const uint DeclSecurity = 0x0e;
        internal const uint Signature = 0x11;
        internal const uint EventMap = 0x12;
        internal const uint Event = 0x14;
        internal const uint PropertyMap = 0x15;
        internal const uint Property = 0x17;
        internal const uint MethodSemantics = 0x18;
        internal const uint MethodImpl = 0x19;
        internal const uint ModuleRef = 0x1a;
        internal const uint TypeSpec = 0x1b;
        internal const uint Assembly = 0x20;
        internal const uint AssemblyRef = 0x23;
        internal const uint File = 0x26;
        internal const uint ExportedType = 0x27;
        internal const uint ManifestResource = 0x28;
        internal const uint NestedClass = 0x29;
        internal const uint GenericParam = 0x2a;
        internal const uint MethodSpec = 0x2b;
        internal const uint GenericParamConstraint = 0x2c;

        internal const uint UserString = 0x70;     // #UserString heap

        // The following values never appear in a token stored in metadata, 
        // they are just helper values to identify the type of a handle.

        internal const uint Blob = 0x71;        // #Blob heap
        internal const uint Guid = 0x72;        // #Guid heap
        internal const uint Namespace = 0x73;   // #String heap but known to be the full name of a namespace

        // #String heap and its modifications (up to 8 string kinds, virtual and non-virtual)
        internal const uint String = 0x7c;
        internal const uint DotTerminatedString = String | ((StringHandleType.DotTerminatedString & ~HeapHandleType.VirtualBit) >> HeapHandleType.OffsetBitCount);
        internal const uint ReservedString1 = String | ((StringHandleType.ReservedString1 & ~HeapHandleType.VirtualBit) >> HeapHandleType.OffsetBitCount); 
        internal const uint ReservedString2 = String | ((StringHandleType.ReservedString2 & ~HeapHandleType.VirtualBit) >> HeapHandleType.OffsetBitCount); 
        internal const uint VirtualString = VirtualBit | String;
        internal const uint WinRTPrefixedString = VirtualBit | String | ((StringHandleType.WinRTPrefixedString & ~HeapHandleType.VirtualBit) >> HeapHandleType.OffsetBitCount);
        internal const uint ReservedVirtualString1 = VirtualBit | String | ((StringHandleType.ReservedString1 & ~HeapHandleType.VirtualBit) >> HeapHandleType.OffsetBitCount);
        internal const uint ReservedVirtualString2 = VirtualBit | String | ((StringHandleType.ReservedString2 & ~HeapHandleType.VirtualBit) >> HeapHandleType.OffsetBitCount);

        internal const uint StringHeapTypeMask = HeapHandleType.NonVirtualTypeMask >> HeapHandleType.OffsetBitCount;
        internal const uint StringOrNamespaceMask = 0x7c;

        internal const uint HeapMask = 0x70;
        internal const uint TypeMask = 0x7F;

        /// <summary>
        /// Use the highest bit to mark tokens that are virtual (synthesized).
        /// We create virtual tokens to represent projected WinMD entities. 
        /// </summary>
        internal const uint VirtualBit = 0x80;

        public static HandleKind ToHandleKind(uint handleType)
        {
            Debug.Assert((handleType & VirtualBit) == 0);

            // Do not surface special string token sub-types (e.g. dot terminated, winrt prefixed) 
            // in public-facing handle type. Pretend that all strings are just plain strings.
            if (handleType > String)
            {
                return HandleKind.String;
            }

            return (HandleKind)handleType;
        }
    }

    internal static class TokenTypeIds
    {
        internal const uint Module = HandleType.Module << RowIdBitCount;
        internal const uint TypeRef = HandleType.TypeRef << RowIdBitCount;
        internal const uint TypeDef = HandleType.TypeDef << RowIdBitCount;
        internal const uint FieldDef = HandleType.FieldDef << RowIdBitCount;
        internal const uint MethodDef = HandleType.MethodDef << RowIdBitCount;
        internal const uint ParamDef = HandleType.ParamDef << RowIdBitCount;
        internal const uint InterfaceImpl = HandleType.InterfaceImpl << RowIdBitCount;
        internal const uint MemberRef = HandleType.MemberRef << RowIdBitCount;
        internal const uint Constant = HandleType.Constant << RowIdBitCount;
        internal const uint CustomAttribute = HandleType.CustomAttribute << RowIdBitCount;
        internal const uint DeclSecurity = HandleType.DeclSecurity << RowIdBitCount;
        internal const uint Signature = HandleType.Signature << RowIdBitCount;
        internal const uint EventMap = HandleType.EventMap << RowIdBitCount;
        internal const uint Event = HandleType.Event << RowIdBitCount;
        internal const uint PropertyMap = HandleType.PropertyMap << RowIdBitCount;
        internal const uint Property = HandleType.Property << RowIdBitCount;
        internal const uint MethodSemantics = HandleType.MethodSemantics << RowIdBitCount;
        internal const uint MethodImpl = HandleType.MethodImpl << RowIdBitCount;
        internal const uint ModuleRef = HandleType.ModuleRef << RowIdBitCount;
        internal const uint TypeSpec = HandleType.TypeSpec << RowIdBitCount;
        internal const uint Assembly = HandleType.Assembly << RowIdBitCount;
        internal const uint AssemblyRef = HandleType.AssemblyRef << RowIdBitCount;
        internal const uint File = HandleType.File << RowIdBitCount;
        internal const uint ExportedType = HandleType.ExportedType << RowIdBitCount;
        internal const uint ManifestResource = HandleType.ManifestResource << RowIdBitCount;
        internal const uint NestedClass = HandleType.NestedClass << RowIdBitCount;
        internal const uint GenericParam = HandleType.GenericParam << RowIdBitCount;
        internal const uint MethodSpec = HandleType.MethodSpec << RowIdBitCount;
        internal const uint GenericParamConstraint = HandleType.GenericParamConstraint << RowIdBitCount;

        internal const uint UserString = HandleType.UserString << RowIdBitCount;

        internal const int RowIdBitCount = 24;
        internal const uint RIDMask = (1 << RowIdBitCount) - 1;
        internal const uint TypeMask = HandleType.TypeMask << RowIdBitCount;

        /// <summary>
        /// Use the highest bit to mark tokens that are virtual (synthesized).
        /// We create virtual tokens to represent projected WinMD entities. 
        /// </summary>
        internal const uint VirtualBit = 0x80000000;

        /// <summary>
        /// Returns true if the token value can escape the metadata reader.
        /// We don't allow virtual tokens and heap tokens other than UserString to escape 
        /// since the token type ids are internal to the reader and not specified by ECMA spec.
        /// 
        /// Spec (Partition III, 1.9 Metadata tokens):
        /// Many CIL instructions are followed by a "metadata token". This is a 4-byte value, that specifies a row in a
        /// metadata table, or a starting byte offset in the User String heap. 
        /// 
        /// For example, a value of 0x02 specifies the TypeDef table; a value of 0x70 specifies the User
        /// String heap.The value corresponds to the number assigned to that metadata table (see Partition II for the full
        /// list of tables) or to 0x70 for the User String heap.The least-significant 3 bytes specify the target row within that
        /// metadata table, or starting byte offset within the User String heap.
        /// </summary>
        internal static bool IsEntityOrUserStringToken(uint vToken)
        {
            return (vToken & TypeMask) <= UserString;
        }

        internal static bool IsEntityToken(uint vToken)
        {
            return (vToken & TypeMask) < UserString;
        }

        internal static bool IsValidRowId(uint rowId)
        {
            return (rowId & ~RIDMask) == 0;
        }

        internal static bool IsValidRowId(int rowId)
        {
            return (rowId & ~RIDMask) == 0;
        }
    }
}
