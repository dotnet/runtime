// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.NativeFormat;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjHeader
    {
        // Contents of the object header
        private IntPtr _objHeaderContents;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DispatchMap
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DispatchMapEntry
        {
            internal ushort _usInterfaceIndex;
            internal ushort _usInterfaceMethodSlot;
            internal ushort _usImplMethodSlot;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct StaticDispatchMapEntry
        {
            // Do not put any other fields before this one. We need StaticDispatchMapEntry* be castable to DispatchMapEntry*.
            internal DispatchMapEntry _entry;
            internal ushort _usContextMapSource;
        }

        private ushort _standardEntryCount; // Implementations on the class
        private ushort _defaultEntryCount; // Default implementations
        private ushort _standardStaticEntryCount; // Implementations on the class (static virtuals)
        private ushort _defaultStaticEntryCount; // Default implementations (static virtuals)
        private DispatchMapEntry _dispatchMap; // at least one entry if any interfaces defined

        public uint NumStandardEntries
        {
            get
            {
                return _standardEntryCount;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _standardEntryCount = checked((ushort)value);
            }
#endif
        }

        public uint NumDefaultEntries
        {
            get
            {
                return _defaultEntryCount;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _defaultEntryCount = checked((ushort)value);
            }
#endif
        }

        public uint NumStandardStaticEntries
        {
            get
            {
                return _standardStaticEntryCount;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _standardStaticEntryCount = checked((ushort)value);
            }
#endif
        }

        public uint NumDefaultStaticEntries
        {
            get
            {
                return _defaultStaticEntryCount;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _defaultStaticEntryCount = checked((ushort)value);
            }
#endif
        }

        public int Size
        {
            get
            {
                return sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort)
                    + sizeof(DispatchMapEntry) * ((int)_standardEntryCount + (int)_defaultEntryCount)
                    + sizeof(StaticDispatchMapEntry) * ((int)_standardStaticEntryCount + (int)_defaultStaticEntryCount);
            }
        }

        public DispatchMapEntry* GetEntry(int index)
        {
            Debug.Assert(index <= _defaultEntryCount + _standardEntryCount);
            return (DispatchMapEntry*)Unsafe.AsPointer(ref Unsafe.Add(ref _dispatchMap, index));
        }

        public DispatchMapEntry* GetStaticEntry(int index)
        {
            Debug.Assert(index <= _defaultStaticEntryCount + _standardStaticEntryCount);
            return (DispatchMapEntry*)(((StaticDispatchMapEntry*)Unsafe.AsPointer(ref Unsafe.Add(ref _dispatchMap, _standardEntryCount + _defaultEntryCount))) + index);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe partial struct MethodTable
    {
#if TARGET_64BIT
        private const int POINTER_SIZE = 8;
        private const int PADDING = 1; // _numComponents is padded by one Int32 to make the first element pointer-aligned
#else
        private const int POINTER_SIZE = 4;
        private const int PADDING = 0;
#endif
        internal const int SZARRAY_BASE_SIZE = POINTER_SIZE + POINTER_SIZE + (1 + PADDING) * 4;

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct RelatedTypeUnion
        {
            // Kinds.CanonicalEEType
            [FieldOffset(0)]
            public MethodTable* _pBaseType;

            // Kinds.ArrayEEType
            [FieldOffset(0)]
            public MethodTable* _pRelatedParameterType;
        }

        private static unsafe class OptionalFieldsReader
        {
            internal static uint GetInlineField(byte* pFields, EETypeOptionalFieldTag eTag, uint uiDefaultValue)
            {
                if (pFields == null)
                    return uiDefaultValue;

                bool isLastField = false;
                while (!isLastField)
                {
                    byte fieldHeader = NativePrimitiveDecoder.ReadUInt8(ref pFields);
                    isLastField = (fieldHeader & 0x80) != 0;
                    EETypeOptionalFieldTag eCurrentTag = (EETypeOptionalFieldTag)(fieldHeader & 0x7f);
                    uint uiCurrentValue = NativePrimitiveDecoder.DecodeUnsigned(ref pFields);

                    // If we found a tag match return the current value.
                    if (eCurrentTag == eTag)
                        return uiCurrentValue;
                }

                // Reached end of stream without getting a match. Field is not present so return default value.
                return uiDefaultValue;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the statically generated data structures use relative pointers.
        /// </summary>
        internal static bool SupportsRelativePointers
        {
            [Intrinsic]
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets a value indicating whether writable data is supported.
        /// </summary>
        internal static bool SupportsWritableData
        {
            get
            {
                // For now just key this off of SupportsRelativePointer to avoid this on both CppCodegen and WASM.
                return SupportsRelativePointers;
            }
        }

        [Intrinsic]
        internal static extern MethodTable* Of<T>();

        // upper ushort is used for Flags
        // lower ushort is used for
        // - component size for strings and arrays,
        // - type arg count for generic type definitions MethodTables,
        // - otherwise holds ExtendedFlags bits
        private uint _uFlags;
        private uint _uBaseSize;
        private RelatedTypeUnion _relatedType;
        private ushort _usNumVtableSlots;
        private ushort _usNumInterfaces;
        private uint _uHashCode;

        // vtable follows

        // These masks and paddings have been chosen so that the ValueTypePadding field can always fit in a byte of data.
        // if the alignment is 8 bytes or less. If the alignment is higher then there may be a need for more bits to hold
        // the rest of the padding data.
        // If paddings of greater than 7 bytes are necessary, then the high bits of the field represent that padding
        private const uint ValueTypePaddingLowMask = 0x7;
        private const uint ValueTypePaddingHighMask = 0xFFFFFF00;
        private const uint ValueTypePaddingMax = 0x07FFFFFF;
        private const int ValueTypePaddingHighShift = 8;
        private const uint ValueTypePaddingAlignmentMask = 0xF8;
        private const int ValueTypePaddingAlignmentShift = 3;

        internal bool HasComponentSize
        {
            get
            {
                // return (_uFlags & (uint)EETypeFlags.HasComponentSizeFlag) != 0;
                return (int)_uFlags < 0;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                if (value)
                {
                    Debug.Assert(ExtendedFlags == 0);
                    _uFlags |= (uint)EETypeFlags.HasComponentSizeFlag;
                }
                else
                {
                    // we should not be un-setting this bit.
                    Debug.Assert(!HasComponentSize);
                }
            }
#endif
        }

        internal ushort ComponentSize
        {
            get
            {
                return HasComponentSize ? (ushort)_uFlags : (ushort)0;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(HasComponentSize);
                _uFlags |= (uint)value;
            }
#endif
        }

        internal ushort GenericParameterCount
        {
            get
            {
                Debug.Assert(IsGenericTypeDefinition);
                return ComponentSize;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsGenericTypeDefinition);
                ComponentSize = value;
            }
#endif
        }

        internal uint Flags
        {
            get
            {
                return _uFlags;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _uFlags = value;
            }
#endif
        }

        internal ushort ExtendedFlags
        {
            get
            {
                return HasComponentSize ? (ushort)0 : (ushort)_uFlags;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(!HasComponentSize);
                Debug.Assert(ExtendedFlags == 0);
                _uFlags |= (uint)value;
            }
#endif
        }

        internal uint RawBaseSize
        {
            get
            {
                return _uBaseSize;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _uBaseSize = value;
            }
#endif
        }

        internal uint BaseSize
        {
            get
            {
                Debug.Assert(IsCanonical || IsArray);
                return _uBaseSize;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _uBaseSize = value;
            }
#endif
        }

        internal ushort NumVtableSlots
        {
            get
            {
                return _usNumVtableSlots;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _usNumVtableSlots = value;
            }
#endif
        }

        internal ushort NumInterfaces
        {
            get
            {
                return _usNumInterfaces;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _usNumInterfaces = value;
            }
#endif
        }

        internal uint HashCode
        {
            get
            {
                return _uHashCode;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _uHashCode = value;
            }
#endif
        }

        private EETypeKind Kind
        {
            get
            {
                return (EETypeKind)(_uFlags & (uint)EETypeFlags.EETypeKindMask);
            }
        }

        internal bool HasOptionalFields
        {
            get
            {
                return (_uFlags & (uint)EETypeFlags.OptionalFieldsFlag) != 0;
            }
        }

        // Mark or determine that a type is generic and one or more of it's type parameters is co- or
        // contra-variant. This only applies to interface and delegate types.
        internal bool HasGenericVariance
        {
            get
            {
                return (_uFlags & (uint)EETypeFlags.GenericVarianceFlag) != 0;
            }
        }

        internal bool IsFinalizable
        {
            get
            {
                return (_uFlags & (uint)EETypeFlags.HasFinalizerFlag) != 0;
            }
        }

        internal bool IsNullable
        {
            get
            {
                return ElementType == EETypeElementType.Nullable;
            }
        }

        internal bool IsCanonical
        {
            get
            {
                return Kind == EETypeKind.CanonicalEEType;
            }
        }

        internal bool IsString
        {
            get
            {
                // String is currently the only non-array type with a non-zero component size.
                return ComponentSize == StringComponentSize.Value && IsCanonical;
            }
        }

        internal bool IsArray
        {
            get
            {
                EETypeElementType elementType = ElementType;
                return elementType == EETypeElementType.Array || elementType == EETypeElementType.SzArray;
            }
        }


        internal int ArrayRank
        {
            get
            {
                Debug.Assert(this.IsArray);

                int boundsSize = (int)this.ParameterizedTypeShape - SZARRAY_BASE_SIZE;
                if (boundsSize > 0)
                {
                    // Multidim array case: Base size includes space for two Int32s
                    // (upper and lower bound) per each dimension of the array.
                    return boundsSize / (2 * sizeof(int));
                }
                return 1;
            }
        }

        internal bool IsSzArray
        {
            get
            {
                Debug.Assert(IsArray);
                return BaseSize == SZARRAY_BASE_SIZE;
            }
        }

        internal bool IsMultiDimensionalArray
        {
            get
            {
                Debug.Assert(HasComponentSize);
                // See comment on RawArrayData for details
                return BaseSize > (uint)(3 * sizeof(IntPtr));
            }
        }

        internal bool IsGeneric
        {
            get
            {
                return (_uFlags & (uint)EETypeFlags.IsGenericFlag) != 0;
            }
        }

        internal bool IsGenericTypeDefinition
        {
            get
            {
                return Kind == EETypeKind.GenericTypeDefEEType;
            }
        }

        internal MethodTable* GenericDefinition
        {
            get
            {
                Debug.Assert(IsGeneric);
                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<IatAwarePointer<MethodTable>>(EETypeField.ETF_GenericDefinition).Value;

                return GetField<IatAwareRelativePointer<MethodTable>>(EETypeField.ETF_GenericDefinition).Value;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsGeneric && IsDynamicType);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_GenericDefinition);
                fixed (MethodTable* pThis = &this)
                {
                    *((MethodTable**)((byte*)pThis + cbOffset)) = value;
                }
            }
#endif
        }

#if TYPE_LOADER_IMPLEMENTATION
        internal static int GetGenericCompositionSize(int numArguments)
        {
            return numArguments * IntPtr.Size;
        }

        internal void SetGenericComposition(IntPtr data)
        {
            Debug.Assert(IsGeneric && IsDynamicType);
            uint cbOffset = GetFieldOffset(EETypeField.ETF_GenericComposition);
            fixed (MethodTable* pThis = &this)
            {
                *((IntPtr*)((byte*)pThis + cbOffset)) = data;
            }
        }
#endif

        internal uint GenericArity
        {
            get
            {
                Debug.Assert(IsGeneric);
                return GenericDefinition->GenericParameterCount;
            }
        }

        internal MethodTableList GenericArguments
        {
            get
            {
                Debug.Assert(IsGeneric);

                void* pField = (byte*)Unsafe.AsPointer(ref this) + GetFieldOffset(EETypeField.ETF_GenericComposition);
                uint arity = GenericArity;

                // If arity is 1, the field value is the component. For arity > 1, components are stored out-of-line
                // and are shared.
                if (IsDynamicType || !SupportsRelativePointers)
                {
                    // This is a full pointer [that points to a list of full pointers]
                    MethodTable* pListStart = arity == 1 ? (MethodTable*)pField : *(MethodTable**)pField;
                    return new MethodTableList(pListStart);
                }
                else
                {
                    // This is a relative pointer [that points to a list of relative pointers]
                    RelativePointer<MethodTable>* pListStart = arity == 1 ?
                        (RelativePointer<MethodTable>*)pField : (RelativePointer<MethodTable>*)((RelativePointer*)pField)->Value;
                    return new MethodTableList(pListStart);
                }
            }
        }

        internal GenericVariance* GenericVariance
        {
            get
            {
                Debug.Assert(IsGeneric || IsGenericTypeDefinition);

                if (!HasGenericVariance)
                    return null;

                if (IsGeneric)
                    return GenericDefinition->GenericVariance;

                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer<GenericVariance>>(EETypeField.ETF_GenericComposition).Value;

                return GetField<RelativePointer<GenericVariance>>(EETypeField.ETF_GenericComposition).Value;
            }
        }

        internal bool IsPointerType
        {
            get
            {
                return ElementType == EETypeElementType.Pointer;
            }
        }

        internal bool IsByRefType
        {
            get
            {
                return ElementType == EETypeElementType.ByRef;
            }
        }

        internal bool IsInterface
        {
            get
            {
                return ElementType == EETypeElementType.Interface;
            }
        }

        internal bool IsByRefLike
        {
            get
            {
                return (RareFlags & EETypeRareFlags.IsByRefLikeFlag) != 0;
            }
        }

        internal bool IsDynamicType
        {
            get
            {
                return (_uFlags & (uint)EETypeFlags.IsDynamicTypeFlag) != 0;
            }
        }

        internal bool IsParameterizedType
        {
            get
            {
                return Kind == EETypeKind.ParameterizedEEType;
            }
        }

        internal bool IsFunctionPointerType
        {
            get
            {
                return Kind == EETypeKind.FunctionPointerEEType;
            }
        }

        // The parameterized type shape defines the particular form of parameterized type that
        // is being represented.
        // Currently, the meaning is a shape of 0 indicates that this is a Pointer,
        // shape of 1 indicates a ByRef, and >=SZARRAY_BASE_SIZE indicates that this is an array.
        // Two types are not equivalent if their shapes do not exactly match.
        internal uint ParameterizedTypeShape
        {
            get
            {
                Debug.Assert(IsParameterizedType);
                return _uBaseSize;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _uBaseSize = value;
            }
#endif
        }

        internal uint NumFunctionPointerParameters
        {
            get
            {
                Debug.Assert(IsFunctionPointerType);
                return _uBaseSize & ~FunctionPointerFlags.FlagsMask;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsFunctionPointerType);
                _uBaseSize = value | (_uBaseSize & FunctionPointerFlags.FlagsMask);
            }
#endif
        }

        internal bool IsUnmanagedFunctionPointer
        {
            get
            {
                Debug.Assert(IsFunctionPointerType);
                return (_uBaseSize & FunctionPointerFlags.IsUnmanaged) != 0;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsFunctionPointerType);
                if (value)
                    _uBaseSize |= FunctionPointerFlags.IsUnmanaged;
                else
                    _uBaseSize &= ~FunctionPointerFlags.IsUnmanaged;
            }
#endif
        }

        internal MethodTableList FunctionPointerParameters
        {
            get
            {
                void* pStart = (byte*)Unsafe.AsPointer(ref this) + GetFieldOffset(EETypeField.ETF_FunctionPointerParameters);
                if (IsDynamicType || !SupportsRelativePointers)
                    return new MethodTableList((MethodTable*)pStart);
                return new MethodTableList((RelativePointer<MethodTable>*)pStart);
            }
        }

        internal MethodTable* FunctionPointerReturnType
        {
            get
            {
                Debug.Assert(IsFunctionPointerType);
                return _relatedType._pRelatedParameterType;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType && IsFunctionPointerType);
                _relatedType._pRelatedParameterType = value;
            }
#endif
        }

        internal bool RequiresAlign8
        {
            get
            {
                return (RareFlags & EETypeRareFlags.RequiresAlign8Flag) != 0;
            }
        }

        internal bool IsIDynamicInterfaceCastable
        {
            get
            {
                return ((ExtendedFlags & (ushort)EETypeFlagsEx.IDynamicInterfaceCastableFlag) != 0);
            }
        }

        internal bool IsValueType
        {
            get
            {
                return ElementType < EETypeElementType.Class;
            }
        }

        // Warning! UNLIKE the similarly named Reflection api, this method also returns "true" for Enums.
        internal bool IsPrimitive
        {
            get
            {
                return ElementType < EETypeElementType.ValueType;
            }
        }

        internal bool HasSealedVTableEntries
        {
            get
            {
                return (_uFlags & (uint)EETypeFlags.HasSealedVTableEntriesFlag) != 0;
            }
        }

        internal bool ContainsGCPointers
        {
            get
            {
                return ((_uFlags & (uint)EETypeFlags.HasPointersFlag) != 0);
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                if (value)
                {
                    _uFlags |= (uint)EETypeFlags.HasPointersFlag;
                }
                else
                {
                    _uFlags &= (uint)~EETypeFlags.HasPointersFlag;
                }
            }
#endif
        }

        internal bool IsHFA
        {
            get
            {
                return (RareFlags & EETypeRareFlags.IsHFAFlag) != 0;
            }
        }

        internal bool IsTrackedReferenceWithFinalizer
        {
            get
            {
                return (ExtendedFlags & (ushort)EETypeFlagsEx.IsTrackedReferenceWithFinalizerFlag) != 0;
            }
        }

        internal uint ValueTypeFieldPadding
        {
            get
            {
                byte* optionalFields = OptionalFieldsPtr;

                // If there are no optional fields then the padding must have been the default, 0.
                if (optionalFields == null)
                    return 0;

                // Get the value from the optional fields. The default is zero if that particular field was not included.
                // The low bits of this field is the ValueType field padding, the rest of the byte is the alignment if present
                uint ValueTypeFieldPaddingData = OptionalFieldsReader.GetInlineField(optionalFields, EETypeOptionalFieldTag.ValueTypeFieldPadding, 0);
                uint padding = ValueTypeFieldPaddingData & ValueTypePaddingLowMask;
                // If there is additional padding, the other bits have that data
                padding |= (ValueTypeFieldPaddingData & ValueTypePaddingHighMask) >> (ValueTypePaddingHighShift - ValueTypePaddingAlignmentShift);
                return padding;
            }
        }

        internal uint ValueTypeSize
        {
            get
            {
                Debug.Assert(IsValueType);
                // get_BaseSize returns the GC size including space for the sync block index field, the MethodTable* and
                // padding for GC heap alignment. Must subtract all of these to get the size used for locals, array
                // elements or fields of another type.
                return BaseSize - ((uint)sizeof(ObjHeader) + (uint)sizeof(MethodTable*) + ValueTypeFieldPadding);
            }
        }

        internal MethodTable** InterfaceMap
        {
            get
            {
                // interface info table starts after the vtable and has _usNumInterfaces entries
                return (MethodTable**)((byte*)Unsafe.AsPointer(ref this) + sizeof(MethodTable) + sizeof(void*) * _usNumVtableSlots);
            }
        }

        internal bool HasDispatchMap
        {
            get
            {
                return (_uFlags & (uint)EETypeFlags.HasDispatchMap) != 0;
            }
        }

        internal DispatchMap* DispatchMap
        {
            get
            {
                if (!HasDispatchMap)
                    return null;

                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer<DispatchMap>>(EETypeField.ETF_DispatchMap).Value;

                return GetField<RelativePointer<DispatchMap>>(EETypeField.ETF_DispatchMap).Value;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType && HasDispatchMap);

                fixed (MethodTable* pThis = &this)
                    *(DispatchMap**)((byte*)pThis + GetFieldOffset(EETypeField.ETF_DispatchMap)) = value;
            }
#endif
        }

        // Get the address of the finalizer method for finalizable types.
        internal IntPtr FinalizerCode
        {
            get
            {
                Debug.Assert(IsFinalizable);

                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer>(EETypeField.ETF_Finalizer).Value;

                return GetField<RelativePointer>(EETypeField.ETF_Finalizer).Value;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType && IsFinalizable);

                fixed (MethodTable* pThis = &this)
                    *(IntPtr*)((byte*)pThis + GetFieldOffset(EETypeField.ETF_Finalizer)) = value;
            }
#endif
        }

        internal MethodTable* BaseType
        {
            get
            {
                if (!IsCanonical)
                {
                    if (IsArray)
                        return GetArrayEEType();
                    else
                        return null;
                }

                return _relatedType._pBaseType;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType);
                Debug.Assert(!IsParameterizedType);
                Debug.Assert(!IsFunctionPointerType);
                Debug.Assert(IsCanonical);
                _relatedType._pBaseType = value;
            }
#endif
        }

        internal MethodTable* NonArrayBaseType
        {
            get
            {
                Debug.Assert(!IsArray, "array type not supported in NonArrayBaseType");
                Debug.Assert(IsCanonical || IsGenericTypeDefinition, "we expect type definitions here");
                Debug.Assert(!IsGenericTypeDefinition || _relatedType._pBaseType == null, "callers assume this would be null for a generic definition");
                return _relatedType._pBaseType;
            }
        }

        internal MethodTable* RawBaseType
        {
            get
            {
                Debug.Assert(!IsParameterizedType, "array type not supported in NonArrayBaseType");
                Debug.Assert(IsCanonical, "we expect canonical types here");
                return _relatedType._pBaseType;
            }
        }

        internal MethodTable* NullableType
        {
            get
            {
                Debug.Assert(IsNullable);
                Debug.Assert(GenericArity == 1);
                return GenericArguments[0];
            }
        }

        /// <summary>
        /// Gets the offset of the value embedded in a Nullable&lt;T&gt;.
        /// </summary>
        internal byte NullableValueOffset
        {
            get
            {
                Debug.Assert(IsNullable);

                // Grab optional fields. If there aren't any then the offset was the default of 1 (immediately after the
                // Nullable's boolean flag).
                byte* optionalFields = OptionalFieldsPtr;
                if (optionalFields == null)
                    return 1;

                // The offset is never zero (Nullable has a boolean there indicating whether the value is valid). So the
                // offset is encoded - 1 to save space. The zero below is the default value if the field wasn't encoded at
                // all.
                return (byte)(OptionalFieldsReader.GetInlineField(optionalFields, EETypeOptionalFieldTag.NullableValueOffset, 0) + 1);
            }
        }

        internal MethodTable* RelatedParameterType
        {
            get
            {
                Debug.Assert(IsParameterizedType);
                return _relatedType._pRelatedParameterType;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType && IsParameterizedType);
                _relatedType._pRelatedParameterType = value;
            }
#endif
        }

        internal unsafe IntPtr* GetVTableStartAddress()
        {
            byte* pResult;

            // EETypes are always in unmanaged memory, so 'leaking' the 'fixed pointer' is safe.
            fixed (MethodTable* pThis = &this)
                pResult = (byte*)pThis;

            pResult += sizeof(MethodTable);
            return (IntPtr*)pResult;
        }

        private static IntPtr FollowRelativePointer(int* pDist)
        {
            int dist = *pDist;
            IntPtr result = (IntPtr)((byte*)pDist + dist);
            return result;
        }

#if TYPE_LOADER_IMPLEMENTATION
        internal
#else
        private
#endif
        void* GetSealedVirtualTable()
        {
            Debug.Assert(HasSealedVTableEntries);

            uint cbSealedVirtualSlotsTypeOffset = GetFieldOffset(EETypeField.ETF_SealedVirtualSlots);
            byte* pThis = (byte*)Unsafe.AsPointer(ref this);
            if (IsDynamicType || !SupportsRelativePointers)
            {
                return *(void**)(pThis + cbSealedVirtualSlotsTypeOffset);
            }
            else
            {
                return (void*)FollowRelativePointer((int*)(pThis + cbSealedVirtualSlotsTypeOffset));
            }
        }

        internal IntPtr GetSealedVirtualSlot(ushort slotNumber)
        {
            void* pSealedVtable = GetSealedVirtualTable();
            if (!SupportsRelativePointers)
            {
                return ((IntPtr*)pSealedVtable)[slotNumber];
            }
            else
            {
                return FollowRelativePointer(&((int*)pSealedVtable)[slotNumber]);
            }
        }

        internal byte* OptionalFieldsPtr
        {
            get
            {
                if (!HasOptionalFields)
                    return null;

                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer<byte>>(EETypeField.ETF_OptionalFieldsPtr).Value;

                return GetField<RelativePointer<byte>>(EETypeField.ETF_OptionalFieldsPtr).Value;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType);

                _uFlags |= (uint)EETypeFlags.OptionalFieldsFlag;

                uint cbOptionalFieldsOffset = GetFieldOffset(EETypeField.ETF_OptionalFieldsPtr);
                fixed (MethodTable* pThis = &this)
                {
                    *(byte**)((byte*)pThis + cbOptionalFieldsOffset) = value;
                }
            }
#endif
        }

        internal MethodTable* DynamicTemplateType
        {
            get
            {
                Debug.Assert(IsDynamicType);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicTemplateType);
                fixed (MethodTable* pThis = &this)
                {
                    return *(MethodTable**)((byte*)pThis + cbOffset);
                }
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicTemplateType);
                fixed (MethodTable* pThis = &this)
                {
                    *(MethodTable**)((byte*)pThis + cbOffset) = value;
                }
            }
#endif
        }

        internal IntPtr DynamicGcStaticsData
        {
            get
            {
                Debug.Assert((RareFlags & EETypeRareFlags.IsDynamicTypeWithGcStatics) != 0);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicGcStatics);
                fixed (MethodTable* pThis = &this)
                {
                    return *(IntPtr*)((byte*)pThis + cbOffset);
                }
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert((RareFlags & EETypeRareFlags.IsDynamicTypeWithGcStatics) != 0);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicGcStatics);
                fixed (MethodTable* pThis = &this)
                {
                    *(IntPtr*)((byte*)pThis + cbOffset) = value;
                }
            }
#endif
        }

        internal IntPtr DynamicNonGcStaticsData
        {
            get
            {
                Debug.Assert((RareFlags & EETypeRareFlags.IsDynamicTypeWithNonGcStatics) != 0);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicNonGcStatics);
                fixed (MethodTable* pThis = &this)
                {
                    return *(IntPtr*)((byte*)pThis + cbOffset);
                }
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert((RareFlags & EETypeRareFlags.IsDynamicTypeWithNonGcStatics) != 0);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicNonGcStatics);
                fixed (MethodTable* pThis = &this)
                {
                    *(IntPtr*)((byte*)pThis + cbOffset) = value;
                }
            }
#endif
        }

        internal IntPtr DynamicThreadStaticsIndex
        {
            get
            {
                Debug.Assert((RareFlags & EETypeRareFlags.IsDynamicTypeWithThreadStatics) != 0);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicThreadStaticOffset);
                fixed (MethodTable* pThis = &this)
                {
                    return *(IntPtr*)((byte*)pThis + cbOffset);
                }
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert((RareFlags & EETypeRareFlags.IsDynamicTypeWithThreadStatics) != 0);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicThreadStaticOffset);
                fixed (MethodTable* pThis = &this)
                {
                    *(IntPtr*)((byte*)pThis + cbOffset) = value;
                }
            }
#endif
        }

        internal TypeManagerHandle TypeManager
        {
            get
            {
                IntPtr typeManagerIndirection;
                if (IsDynamicType || !SupportsRelativePointers)
                    typeManagerIndirection = GetField<Pointer>(EETypeField.ETF_TypeManagerIndirection).Value;
                else
                    typeManagerIndirection = GetField<RelativePointer>(EETypeField.ETF_TypeManagerIndirection).Value;

                return *(TypeManagerHandle*)typeManagerIndirection;
            }
        }
#if TYPE_LOADER_IMPLEMENTATION
        internal IntPtr PointerToTypeManager
        {
            get
            {
                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer>(EETypeField.ETF_TypeManagerIndirection).Value;

                return GetField<RelativePointer>(EETypeField.ETF_TypeManagerIndirection).Value;
            }
            set
            {
                Debug.Assert(IsDynamicType);
                uint cbOffset = GetFieldOffset(EETypeField.ETF_TypeManagerIndirection);
                // This is always a pointer to a pointer to a type manager
                *(TypeManagerHandle**)((byte*)Unsafe.AsPointer(ref this) + cbOffset) = (TypeManagerHandle*)value;
            }
        }
#endif

        /// <summary>
        /// Gets a pointer to a segment of writable memory associated with this MethodTable.
        /// The purpose of the segment is controlled by the class library. The runtime doesn't
        /// use this memory for any purpose.
        /// </summary>
        internal IntPtr WritableData
        {
            get
            {
                Debug.Assert(SupportsWritableData);

                uint offset = GetFieldOffset(EETypeField.ETF_WritableData);

                if (!IsDynamicType)
                    return GetField<RelativePointer>(offset).Value;
                else
                    return GetField<Pointer>(offset).Value;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType && SupportsWritableData);

                uint cbOffset = GetFieldOffset(EETypeField.ETF_WritableData);
                *(IntPtr*)((byte*)Unsafe.AsPointer(ref this) + cbOffset) = value;
            }
#endif
        }

        internal unsafe EETypeRareFlags RareFlags
        {
            get
            {
                // If there are no optional fields then none of the rare flags have been set.
                // Get the flags from the optional fields. The default is zero if that particular field was not included.
                return HasOptionalFields ? (EETypeRareFlags)OptionalFieldsReader.GetInlineField(OptionalFieldsPtr, EETypeOptionalFieldTag.RareFlags, 0) : 0;
            }
        }

        internal int FieldAlignmentRequirement
        {
            get
            {
                byte* optionalFields = OptionalFieldsPtr;

                // If there are no optional fields then the alignment must have been the default, IntPtr.Size.
                // (This happens for all reference types, and for valuetypes with default alignment and no padding)
                if (optionalFields == null)
                    return IntPtr.Size;

                // Get the value from the optional fields. The default is zero if that particular field was not included.
                // The low bits of this field is the ValueType field padding, the rest of the value is the alignment if present
                uint alignmentValue = (OptionalFieldsReader.GetInlineField(optionalFields, EETypeOptionalFieldTag.ValueTypeFieldPadding, 0) & ValueTypePaddingAlignmentMask) >> ValueTypePaddingAlignmentShift;

                // Alignment is stored as 1 + the log base 2 of the alignment, except a 0 indicates standard pointer alignment.
                if (alignmentValue == 0)
                    return IntPtr.Size;
                else
                    return 1 << ((int)alignmentValue - 1);
            }
        }

        internal EETypeElementType ElementType
        {
            get
            {
                return (EETypeElementType)((_uFlags >> (byte)EETypeFlags.ElementTypeShift) &
                    ((uint)EETypeFlags.ElementTypeMask >> (byte)EETypeFlags.ElementTypeShift));
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _uFlags = (_uFlags & ~(uint)EETypeFlags.ElementTypeMask) | ((uint)value << (byte)EETypeFlags.ElementTypeShift);
            }
#endif
        }

        public bool HasCctor
        {
            get
            {
                return (RareFlags & EETypeRareFlags.HasCctorFlag) != 0;
            }
        }

        public uint GetFieldOffset(EETypeField eField)
        {
            // First part of MethodTable consists of the fixed portion followed by the vtable.
            uint cbOffset = (uint)(sizeof(MethodTable) + (IntPtr.Size * _usNumVtableSlots));

            // Then we have the interface map.
            if (eField == EETypeField.ETF_InterfaceMap)
            {
                Debug.Assert(NumInterfaces > 0);
                return cbOffset;
            }
            cbOffset += (uint)(sizeof(MethodTable*) * NumInterfaces);

            uint relativeOrFullPointerOffset = (IsDynamicType || !SupportsRelativePointers ? (uint)IntPtr.Size : 4);

            // Followed by the type manager indirection cell.
            if (eField == EETypeField.ETF_TypeManagerIndirection)
            {
                return cbOffset;
            }
            cbOffset += relativeOrFullPointerOffset;

            // Followed by writable data.
            if (SupportsWritableData)
            {
                if (eField == EETypeField.ETF_WritableData)
                {
                    return cbOffset;
                }
                cbOffset += relativeOrFullPointerOffset;
            }

            // Followed by pointer to the dispatch map
            if (eField == EETypeField.ETF_DispatchMap)
            {
                Debug.Assert(HasDispatchMap);
                return cbOffset;
            }
            if (HasDispatchMap)
                cbOffset += relativeOrFullPointerOffset;

            // Followed by the pointer to the finalizer method.
            if (eField == EETypeField.ETF_Finalizer)
            {
                Debug.Assert(IsFinalizable);
                return cbOffset;
            }
            if (IsFinalizable)
                cbOffset += relativeOrFullPointerOffset;

            // Followed by the pointer to the optional fields.
            if (eField == EETypeField.ETF_OptionalFieldsPtr)
            {
                Debug.Assert(HasOptionalFields);
                return cbOffset;
            }
            if (HasOptionalFields)
                cbOffset += relativeOrFullPointerOffset;

            // Followed by the pointer to the sealed virtual slots
            if (eField == EETypeField.ETF_SealedVirtualSlots)
                return cbOffset;

            // in the case of sealed vtable entries on static types, we have a UInt sized relative pointer
            if (HasSealedVTableEntries)
                cbOffset += relativeOrFullPointerOffset;

            if (eField == EETypeField.ETF_GenericDefinition)
            {
                Debug.Assert(IsGeneric);
                return cbOffset;
            }
            if (IsGeneric)
            {
                cbOffset += relativeOrFullPointerOffset;
            }

            if (eField == EETypeField.ETF_GenericComposition)
            {
                Debug.Assert(IsGeneric || (IsGenericTypeDefinition && HasGenericVariance));
                return cbOffset;
            }
            if (IsGeneric || (IsGenericTypeDefinition && HasGenericVariance))
            {
                cbOffset += relativeOrFullPointerOffset;
            }

            if (eField == EETypeField.ETF_FunctionPointerParameters)
            {
                Debug.Assert(IsFunctionPointerType);
                return cbOffset;
            }
            if (IsFunctionPointerType)
            {
                cbOffset += NumFunctionPointerParameters * relativeOrFullPointerOffset;
            }

            if (eField == EETypeField.ETF_DynamicTemplateType)
            {
                Debug.Assert(IsDynamicType);
                return cbOffset;
            }
            if (IsDynamicType)
                cbOffset += (uint)IntPtr.Size;

            EETypeRareFlags rareFlags = RareFlags;
            if (eField == EETypeField.ETF_DynamicGcStatics)
            {
                Debug.Assert((rareFlags & EETypeRareFlags.IsDynamicTypeWithGcStatics) != 0);
                return cbOffset;
            }
            if ((rareFlags & EETypeRareFlags.IsDynamicTypeWithGcStatics) != 0)
                cbOffset += (uint)IntPtr.Size;

            if (eField == EETypeField.ETF_DynamicNonGcStatics)
            {
                Debug.Assert((rareFlags & EETypeRareFlags.IsDynamicTypeWithNonGcStatics) != 0);
                return cbOffset;
            }
            if ((rareFlags & EETypeRareFlags.IsDynamicTypeWithNonGcStatics) != 0)
                cbOffset += (uint)IntPtr.Size;

            if (eField == EETypeField.ETF_DynamicThreadStaticOffset)
            {
                Debug.Assert((rareFlags & EETypeRareFlags.IsDynamicTypeWithThreadStatics) != 0);
                return cbOffset;
            }

            Debug.Assert(false, "Unknown MethodTable field type");
            return 0;
        }

        public ref T GetField<T>(EETypeField eField)
        {
            return ref Unsafe.As<byte, T>(ref *((byte*)Unsafe.AsPointer(ref this) + GetFieldOffset(eField)));
        }

        public ref T GetField<T>(uint offset)
        {
            return ref Unsafe.As<byte, T>(ref *((byte*)Unsafe.AsPointer(ref this) + offset));
        }

#if TYPE_LOADER_IMPLEMENTATION
        internal static uint GetSizeofEEType(
            ushort cVirtuals,
            ushort cInterfaces,
            bool fHasDispatchMap,
            bool fHasFinalizer,
            bool fRequiresOptionalFields,
            bool fHasSealedVirtuals,
            bool fHasGenericInfo,
            int cFunctionPointerTypeParameters,
            bool fHasNonGcStatics,
            bool fHasGcStatics,
            bool fHasThreadStatics)
        {
            return (uint)(sizeof(MethodTable) +
                (IntPtr.Size * cVirtuals) +
                (sizeof(MethodTable*) * cInterfaces) +
                sizeof(IntPtr) + // TypeManager
                (SupportsWritableData ? sizeof(IntPtr) : 0) + // WritableData
                (fHasDispatchMap ? sizeof(UIntPtr) : 0) +
                (fHasFinalizer ? sizeof(UIntPtr) : 0) +
                (fRequiresOptionalFields ? sizeof(IntPtr) : 0) +
                (fHasSealedVirtuals ? sizeof(IntPtr) : 0) +
                cFunctionPointerTypeParameters * sizeof(IntPtr) +
                (fHasGenericInfo ? sizeof(IntPtr)*2 : 0) + // pointers to GenericDefinition and GenericComposition
                (fHasNonGcStatics ? sizeof(IntPtr) : 0) + // pointer to data
                (fHasGcStatics ? sizeof(IntPtr) : 0) +  // pointer to data
                (fHasThreadStatics ? sizeof(IntPtr) : 0)); // threadstatic index cell
        }
#endif
    }

    // Wrapper around pointers
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Pointer
    {
        private readonly IntPtr _value;

        public IntPtr Value
        {
            get
            {
                return _value;
            }
        }
    }

    // Wrapper around pointers
    [StructLayout(LayoutKind.Sequential)]
    internal readonly unsafe struct Pointer<T> where T : unmanaged
    {
        private readonly T* _value;

        public T* Value
        {
            get
            {
                return _value;
            }
        }
    }

    // Wrapper around pointers that might be indirected through IAT
    [StructLayout(LayoutKind.Sequential)]
    internal readonly unsafe struct IatAwarePointer<T> where T : unmanaged
    {
        private readonly T* _value;

        public T* Value
        {
            get
            {
                if (((int)_value & IndirectionConstants.IndirectionCellPointer) == 0)
                    return _value;
                return *(T**)((byte*)_value - IndirectionConstants.IndirectionCellPointer);
            }
        }
    }

    // Wrapper around relative pointers
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct RelativePointer
    {
        private readonly int _value;

        public unsafe IntPtr Value
        {
            get
            {
                return (IntPtr)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + _value);
            }
        }
    }

    // Wrapper around relative pointers
    [StructLayout(LayoutKind.Sequential)]
    internal readonly unsafe struct RelativePointer<T> where T : unmanaged
    {
        private readonly int _value;

        public T* Value
        {
            get
            {
                return (T*)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + _value);
            }
        }
    }

    // Wrapper around relative pointers that might be indirected through IAT
    [StructLayout(LayoutKind.Sequential)]
    internal readonly unsafe struct IatAwareRelativePointer<T> where T : unmanaged
    {
        private readonly int _value;

        public T* Value
        {
            get
            {
                if ((_value & IndirectionConstants.IndirectionCellPointer) == 0)
                {
                    return (T*)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + _value);
                }
                else
                {
                    return *(T**)((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _value)) + (_value & ~IndirectionConstants.IndirectionCellPointer));
                }
            }
        }
    }

    // Abstracts a list of MethodTable pointers that could either be relative
    // pointers or full pointers. We store the IsRelative bit in the lowest
    // bit so this assumes the list is at least 2 byte aligned.
    internal readonly unsafe struct MethodTableList
    {
        private const int IsRelative = 1;

        private readonly void* _pFirst;

        public MethodTableList(MethodTable* pFirst)
        {
            // If the first element is not aligned, we don't have the spare bit we need
            Debug.Assert(((nint)pFirst & IsRelative) == 0);
            _pFirst = pFirst;
        }

        public MethodTableList(RelativePointer<MethodTable>* pFirst)
        {
            // If the first element is not aligned, we don't have the spare bit we need
            Debug.Assert(((nint)pFirst & IsRelative) == 0);
            _pFirst = (void*)((nint)pFirst | IsRelative);
        }

        public MethodTable* this[int index]
        {
            get
            {
                if (((nint)_pFirst & IsRelative) != 0)
                    return (((RelativePointer<MethodTable>*)((nint)_pFirst - IsRelative)) + index)->Value;

                return *((MethodTable**)_pFirst + index);
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(((nint)_pFirst & IsRelative) == 0);
                *((MethodTable**)_pFirst + index) = value;
            }
#endif
        }
    }
}
