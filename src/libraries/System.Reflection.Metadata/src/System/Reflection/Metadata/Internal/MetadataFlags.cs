// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        Plain = 0,
        WinRTPrefixed = 1,
        DotTerminated = 2,
    }

    internal enum NamespaceKind : byte
    {
        Plain = 0,
        Synthetic = 1,
    }

    internal static class TokenTypeIds
    {
        internal const uint Module = 0x00000000;
        internal const uint TypeRef = 0x01000000;
        internal const uint TypeDef = 0x02000000;
        internal const uint FieldDef = 0x04000000;
        internal const uint MethodDef = 0x06000000;
        internal const uint ParamDef = 0x08000000;
        internal const uint InterfaceImpl = 0x09000000;
        internal const uint MemberRef = 0x0a000000;
        internal const uint Constant = 0x0b000000;
        internal const uint CustomAttribute = 0x0c000000;
        internal const uint DeclSecurity = 0x0e000000;
        internal const uint Signature = 0x11000000;
        internal const uint EventMap = 0x12000000;
        internal const uint Event = 0x14000000;
        internal const uint PropertyMap = 0x15000000;
        internal const uint Property = 0x17000000;
        internal const uint MethodSemantics = 0x18000000;
        internal const uint MethodImpl = 0x19000000;
        internal const uint ModuleRef = 0x1a000000;
        internal const uint TypeSpec = 0x1b000000;
        internal const uint Assembly = 0x20000000;
        internal const uint AssemblyRef = 0x23000000;
        internal const uint File = 0x26000000;
        internal const uint ExportedType = 0x27000000;
        internal const uint ManifestResource = 0x28000000;
        internal const uint NestedClass = 0x29000000;
        internal const uint GenericParam = 0x2a000000;
        internal const uint MethodSpec = 0x2b000000;
        internal const uint GenericParamConstraint = 0x2c000000;

        internal const uint UserString = 0x70000000;     // #UserString heap

        // The following values never appear in a token stored in metadata, 
        // they are just helper values to identify the type of a handle.

        internal const uint Blob = 0x71000000;        // #Blob heap
        internal const uint Guid = 0x72000000;        // #Guid heap

        // #String heap and its modifications
        internal const uint String = 0x78000000;               // #String heap
        internal const uint WinRTPrefixedString = 0x79000000;  // #String heap with <WinRT> prefix
        internal const uint DotTerminatedString = 0x7a000000;  // #String heap that treats '.' as a string terminator in addition to '\0'
        // internal const uint ReservedString = 0x7b000000;    // [reserved] can only be used for a new string kind.
        internal const uint MaxString = DotTerminatedString;

        internal const uint Namespace = 0x7c000000;              // Namespace handle for namespace with types of its own
        internal const uint SyntheticNamespace = 0x7d000000;     // Namespace handle for namespace with child namespaces but no types of its own
        // internal const uint Reserved1Namespace = 0x7e000000;  // [reserved] can only be used for a new namespace kind
        // internal const uint Reserved2Namespace = 0x7f000000;  // [reserved] can only be used for a new namespace kind
        internal const uint MaxNamespace = SyntheticNamespace;

        internal const uint StringOrNamespaceKindMask = 0x03000000;

        internal const uint HeapMask = 0x70000000;
        internal const uint RIDMask = 0x00FFFFFF;
        internal const uint TableTokenTypeMask = 0x5F000000;
        internal const uint TokenTypeMask = 0x7F000000;

        /// <summary>
        /// Use the highest bit to mark tokens that are virtual (synthesized).
        /// We create virtual tokens to represent projected WinMD entities. 
        /// </summary>
        internal const uint VirtualTokenMask = 1U << 31;

        internal const uint VirtualBitAndRowIdMask = VirtualTokenMask | RIDMask;

        internal const int RowIdBitCount = 24;

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
        internal static bool IsEcmaToken(uint value)
        {
            return (value & TokenTypeMask) <= UserString;
        }

        internal static bool IsValidRowId(uint rowId)
        {
            return (rowId & ~RIDMask) == 0;
        }

        internal static int CompareTokens(uint t1, uint t2)
        {
            // all virtual tokens will be sorted after non-virtual tokens
            return (int)((t1 & RIDMask) | ((t1 & VirtualTokenMask) >> 3)) -
                   (int)((t2 & RIDMask) | ((t2 & VirtualTokenMask) >> 3));
        }
    }
}
