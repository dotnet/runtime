// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        Document = 1UL << TableIndex.Document,
        MethodDebugInformation = 1UL << TableIndex.MethodDebugInformation,
        LocalScope = 1UL << TableIndex.LocalScope,
        LocalVariable = 1UL << TableIndex.LocalVariable,
        LocalConstant = 1UL << TableIndex.LocalConstant,
        ImportScope = 1UL << TableIndex.ImportScope,
        StateMachineMethod = 1UL << TableIndex.StateMachineMethod,
        CustomDebugInformation = 1UL << TableIndex.CustomDebugInformation,

        PtrTables =
            FieldPtr
          | MethodPtr
          | ParamPtr
          | EventPtr
          | PropertyPtr,

        EncTables =
            EnCLog
          | EnCMap,

        TypeSystemTables =
            PtrTables 
          | EncTables
          | Module
          | TypeRef
          | TypeDef
          | Field
          | MethodDef
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
          | Event
          | PropertyMap
          | Property
          | MethodSemantics
          | MethodImpl
          | ModuleRef
          | TypeSpec
          | ImplMap
          | FieldRva
          | Assembly
          | AssemblyRef
          | File
          | ExportedType
          | ManifestResource
          | NestedClass
          | GenericParam
          | MethodSpec
          | GenericParamConstraint,

        DebugTables =
            Document
          | MethodDebugInformation
          | LocalScope
          | LocalVariable
          | LocalConstant
          | ImportScope
          | StateMachineMethod
          | CustomDebugInformation,

        AllTables = 
            TypeSystemTables |
            DebugTables,

        ValidPortablePdbExternalTables =
            TypeSystemTables & ~PtrTables & ~EncTables
    }

    internal enum HeapSizes : byte
    {
        StringHeapLarge = 0x01, // 4 byte uint indexes used for string heap offsets
        GuidHeapLarge = 0x02,   // 4 byte uint indexes used for GUID heap offsets
        BlobHeapLarge = 0x04,   // 4 byte uint indexes used for Blob heap offsets
        ExtraData = 0x40,       // Indicates that there is an extra 4 bytes of data immediately after the row counts
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
        // The 3 high bits above the offset that specify the full string type (including virtual bit)
        internal const uint TypeMask = ~(HeapHandleType.OffsetMask);

        // The string type bits excluding the virtual bit.
        internal const uint NonVirtualTypeMask = TypeMask & ~(HeapHandleType.VirtualBit);

        // NUL-terminated UTF8 string on a #String heap.
        internal const uint String = (0 << HeapHandleType.OffsetBitCount);

        // String on #String heap whose terminator is NUL and '.', whichever comes first.
        internal const uint DotTerminatedString = (1 << HeapHandleType.OffsetBitCount);

        // Reserved values that can be used for future strings:
        internal const uint ReservedString1 = (2 << HeapHandleType.OffsetBitCount);
        internal const uint ReservedString2 = (3 << HeapHandleType.OffsetBitCount);

        // Virtual string identified by a virtual index
        internal const uint VirtualString = HeapHandleType.VirtualBit | (0 << HeapHandleType.OffsetBitCount);

        // Virtual string whose value is a "<WinRT>" prefixed string found at the specified heap offset.
        internal const uint WinRTPrefixedString = HeapHandleType.VirtualBit | (1 << HeapHandleType.OffsetBitCount);

        // Reserved virtual strings that can be used in future:
        internal const uint ReservedVirtualString1 = HeapHandleType.VirtualBit | (2 << HeapHandleType.OffsetBitCount);
        internal const uint ReservedVirtualString2 = HeapHandleType.VirtualBit | (3 << HeapHandleType.OffsetBitCount);
    }

    internal static class HeapHandleType
    {
        // Heap offset values are limited to 29 bits (max compressed integer)
        internal const int OffsetBitCount = 29;
        internal const uint OffsetMask = (1 << OffsetBitCount) - 1;
        internal const uint VirtualBit = 0x80000000;

        internal static bool IsValidHeapOffset(uint offset)
        {
            return (offset & ~OffsetMask) == 0;
        }
    }

    /// <summary>
    /// These constants are all in the byte range and apply to the interpretation of <see cref="Handle.VType"/>,
    /// </summary>
    internal static class HandleType
    {
        internal const uint Module = (uint)TableIndex.Module;
        internal const uint TypeRef = (uint)TableIndex.TypeRef;
        internal const uint TypeDef = (uint)TableIndex.TypeDef;
        internal const uint FieldDef = (uint)TableIndex.Field;
        internal const uint MethodDef = (uint)TableIndex.MethodDef;
        internal const uint ParamDef = (uint)TableIndex.Param;
        internal const uint InterfaceImpl = (uint)TableIndex.InterfaceImpl;
        internal const uint MemberRef = (uint)TableIndex.MemberRef;
        internal const uint Constant = (uint)TableIndex.Constant;
        internal const uint CustomAttribute = (uint)TableIndex.CustomAttribute;
        internal const uint DeclSecurity = (uint)TableIndex.DeclSecurity;
        internal const uint Signature = (uint)TableIndex.StandAloneSig;
        internal const uint EventMap = (uint)TableIndex.EventMap;
        internal const uint Event = (uint)TableIndex.Event;
        internal const uint PropertyMap = (uint)TableIndex.PropertyMap;
        internal const uint Property = (uint)TableIndex.Property;
        internal const uint MethodSemantics = (uint)TableIndex.MethodSemantics;
        internal const uint MethodImpl = (uint)TableIndex.MethodImpl;
        internal const uint ModuleRef = (uint)TableIndex.ModuleRef;
        internal const uint TypeSpec = (uint)TableIndex.TypeSpec;
        internal const uint Assembly = (uint)TableIndex.Assembly;
        internal const uint AssemblyRef = (uint)TableIndex.AssemblyRef;
        internal const uint File = (uint)TableIndex.File;
        internal const uint ExportedType = (uint)TableIndex.ExportedType;
        internal const uint ManifestResource = (uint)TableIndex.ManifestResource;
        internal const uint NestedClass = (uint)TableIndex.NestedClass;
        internal const uint GenericParam = (uint)TableIndex.GenericParam;
        internal const uint MethodSpec = (uint)TableIndex.MethodSpec;
        internal const uint GenericParamConstraint = (uint)TableIndex.GenericParamConstraint;

        // debug tables:
        internal const uint Document = (uint)TableIndex.Document;
        internal const uint MethodDebugInformation = (uint)TableIndex.MethodDebugInformation;
        internal const uint LocalScope = (uint)TableIndex.LocalScope;
        internal const uint LocalVariable = (uint)TableIndex.LocalVariable;
        internal const uint LocalConstant = (uint)TableIndex.LocalConstant;
        internal const uint ImportScope = (uint)TableIndex.ImportScope;
        internal const uint AsyncMethod = (uint)TableIndex.StateMachineMethod;
        internal const uint CustomDebugInformation = (uint)TableIndex.CustomDebugInformation;

        internal const uint UserString = 0x70;     // #UserString heap

        // The following values never appear in a token stored in metadata, 
        // they are just helper values to identify the type of a handle.
        // Note, however, that even though they do not come from the spec,
        // they are surfaced as public constants via HandleKind enum and 
        // therefore cannot change!

        internal const uint Blob = 0x71;        // #Blob heap
        internal const uint Guid = 0x72;        // #Guid heap

        // #String heap and its modifications
        //
        // Multiple values are reserved for string handles so that we can encode special
        // handling with more than just the virtual bit. See StringHandleType for how
        // the two extra bits are actually interpreted. The extra String1,2,3 values here are 
        // not used directly, but serve as a reminder that they are not available for use
        // by another handle type.
        internal const uint String  = 0x78;
        internal const uint String1 = 0x79;
        internal const uint String2 = 0x7a;
        internal const uint String3 = 0x7b;

        // Namespace handles also have offsets into the #String heap (when non-virtual)
        // to their full name. However, this is an implementation detail and they are
        // surfaced with first-class HandleKind.Namespace and strongly-typed NamespaceHandle.
        internal const uint Namespace = 0x7c;

        internal const uint HeapMask = 0x70;
        internal const uint TypeMask = 0x7F;

        /// <summary>
        /// Use the highest bit to mark tokens that are virtual (synthesized).
        /// We create virtual tokens to represent projected WinMD entities. 
        /// </summary>
        internal const uint VirtualBit = 0x80;

        /// <summary>
        /// In the case of string handles, the two lower bits that (in addition to the 
        /// virtual bit not included in this mask) encode how to obtain the string value.
        /// </summary>
        internal const uint NonVirtualStringTypeMask = 0x03;
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

        // debug tables:
        internal const uint Document = HandleType.Document << RowIdBitCount;
        internal const uint MethodDebugInformation = HandleType.MethodDebugInformation << RowIdBitCount;
        internal const uint LocalScope = HandleType.LocalScope << RowIdBitCount;
        internal const uint LocalVariable = HandleType.LocalVariable << RowIdBitCount;
        internal const uint LocalConstant = HandleType.LocalConstant << RowIdBitCount;
        internal const uint ImportScope = HandleType.ImportScope << RowIdBitCount;
        internal const uint AsyncMethod = HandleType.AsyncMethod << RowIdBitCount;
        internal const uint CustomDebugInformation = HandleType.CustomDebugInformation << RowIdBitCount;

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
