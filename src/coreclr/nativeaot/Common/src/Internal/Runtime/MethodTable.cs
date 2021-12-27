// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.NativeFormat;
using Internal.Runtime.CompilerServices;

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
    internal unsafe struct EEInterfaceInfo
    {
        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct InterfaceTypeUnion
        {
            [FieldOffset(0)]
            public MethodTable* _pInterfaceEEType;
            [FieldOffset(0)]
            public MethodTable** _ppInterfaceEETypeViaIAT;
        }

        private InterfaceTypeUnion _interfaceType;

        internal MethodTable* InterfaceType
        {
            get
            {
                if ((unchecked((uint)_interfaceType._pInterfaceEEType) & IndirectionConstants.IndirectionCellPointer) != 0)
                {
#if TARGET_64BIT
                    MethodTable** ppInterfaceEETypeViaIAT = (MethodTable**)(((ulong)_interfaceType._ppInterfaceEETypeViaIAT) - IndirectionConstants.IndirectionCellPointer);
#else
                    MethodTable** ppInterfaceEETypeViaIAT = (MethodTable**)(((uint)_interfaceType._ppInterfaceEETypeViaIAT) - IndirectionConstants.IndirectionCellPointer);
#endif
                    return *ppInterfaceEETypeViaIAT;
                }

                return _interfaceType._pInterfaceEEType;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _interfaceType._pInterfaceEEType = value;
            }
#endif
        }
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

        private ushort _standardEntryCount; // Implementations on the class
        private ushort _defaultEntryCount; // Default implementations
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

        public int Size
        {
            get
            {
                return sizeof(ushort) + sizeof(ushort) + sizeof(DispatchMapEntry) * ((int)_standardEntryCount + (int)_defaultEntryCount);
            }
        }

        public DispatchMapEntry* this[int index]
        {
            get
            {
                return (DispatchMapEntry*)Unsafe.AsPointer(ref Unsafe.Add(ref _dispatchMap, index));
            }
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
            [FieldOffset(0)]
            public MethodTable** _ppBaseTypeViaIAT;

            // Kinds.ClonedEEType
            [FieldOffset(0)]
            public MethodTable* _pCanonicalType;
            [FieldOffset(0)]
            public MethodTable** _ppCanonicalTypeViaIAT;

            // Kinds.ArrayEEType
            [FieldOffset(0)]
            public MethodTable* _pRelatedParameterType;
            [FieldOffset(0)]
            public MethodTable** _ppRelatedParameterTypeViaIAT;
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

        private ushort _usComponentSize;
        private ushort _usFlags;
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

        internal ushort ComponentSize
        {
            get
            {
                return _usComponentSize;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _usComponentSize = value;
            }
#endif
        }

        internal ushort GenericArgumentCount
        {
            get
            {
                Debug.Assert(IsGenericTypeDefinition);
                return _usComponentSize;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsGenericTypeDefinition);
                _usComponentSize = value;
            }
#endif
        }

        internal ushort Flags
        {
            get
            {
                return _usFlags;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _usFlags = value;
            }
#endif
        }

        internal uint BaseSize
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
                return (EETypeKind)(_usFlags & (ushort)EETypeFlags.EETypeKindMask);
            }
        }

        internal bool HasOptionalFields
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.OptionalFieldsFlag) != 0);
            }
        }

        // Mark or determine that a type is generic and one or more of it's type parameters is co- or
        // contra-variant. This only applies to interface and delegate types.
        internal bool HasGenericVariance
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.GenericVarianceFlag) != 0);
            }
        }

        internal bool IsFinalizable
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.HasFinalizerFlag) != 0);
            }
        }

        internal bool IsNullable
        {
            get
            {
                return ElementType == EETypeElementType.Nullable;
            }
        }

        internal bool IsCloned
        {
            get
            {
                return Kind == EETypeKind.ClonedEEType;
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
                return ComponentSize == StringComponentSize.Value && !IsArray && !IsGenericTypeDefinition;
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
                return ElementType == EETypeElementType.SzArray;
            }
        }

        internal bool IsGeneric
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.IsGenericFlag) != 0);
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

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct GenericComposition
        {
            public readonly ushort Arity;

            private readonly EETypeRef _genericArgument1;
            public EETypeRef* GenericArguments
            {
                get
                {
                    return (EETypeRef*)Unsafe.AsPointer(ref Unsafe.AsRef(in _genericArgument1));
                }
            }

            public GenericVariance* GenericVariance
            {
                get
                {
                    // Generic variance directly follows the last generic argument
                    return (GenericVariance*)(GenericArguments + Arity);
                }
            }
        }

#if TYPE_LOADER_IMPLEMENTATION
        internal static int GetGenericCompositionSize(int numArguments, bool hasVariance)
        {
            return IntPtr.Size
                + numArguments * IntPtr.Size
                + (hasVariance ? numArguments * sizeof(GenericVariance) : 0);
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
                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer<GenericComposition>>(EETypeField.ETF_GenericComposition).Value->Arity;

                return GetField<RelativePointer<GenericComposition>>(EETypeField.ETF_GenericComposition).Value->Arity;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType);
                // GenericComposition is a readonly struct, so we just blit the bytes over. Asserts guard changes to the layout.
                *((ushort*)GetField<Pointer<GenericComposition>>(EETypeField.ETF_GenericComposition).Value) = checked((ushort)value);
                Debug.Assert(GenericArity == (ushort)value);
            }
#endif
        }

        internal EETypeRef* GenericArguments
        {
            get
            {
                Debug.Assert(IsGeneric);
                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer<GenericComposition>>(EETypeField.ETF_GenericComposition).Value->GenericArguments;

                return GetField<RelativePointer<GenericComposition>>(EETypeField.ETF_GenericComposition).Value->GenericArguments;
            }
        }

        internal GenericVariance* GenericVariance
        {
            get
            {
                Debug.Assert(IsGeneric);

                if (!HasGenericVariance)
                    return null;

                if (IsDynamicType || !SupportsRelativePointers)
                    return GetField<Pointer<GenericComposition>>(EETypeField.ETF_GenericComposition).Value->GenericVariance;

                return GetField<RelativePointer<GenericComposition>>(EETypeField.ETF_GenericComposition).Value->GenericVariance;
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

        internal bool IsAbstract
        {
            get
            {
                return IsInterface || (RareFlags & EETypeRareFlags.IsAbstractClassFlag) != 0;
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
                return (_usFlags & (ushort)EETypeFlags.IsDynamicTypeFlag) != 0;
            }
        }

        internal bool HasDynamicallyAllocatedDispatchMap
        {
            get
            {
                return (RareFlags & EETypeRareFlags.HasDynamicallyAllocatedDispatchMapFlag) != 0;
            }
        }

        internal bool IsParameterizedType
        {
            get
            {
                return Kind == EETypeKind.ParameterizedEEType;
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
                return _uBaseSize;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _uBaseSize = value;
            }
#endif
        }

        internal bool IsRelatedTypeViaIAT
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.RelatedTypeViaIATFlag) != 0);
            }
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
                return ((_usFlags & (ushort)EETypeFlags.IDynamicInterfaceCastableFlag) != 0);
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

        internal bool HasGCPointers
        {
            get
            {
                return ((_usFlags & (ushort)EETypeFlags.HasPointersFlag) != 0);
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                if (value)
                {
                    _usFlags |= (ushort)EETypeFlags.HasPointersFlag;
                }
                else
                {
                    _usFlags &= (ushort)~EETypeFlags.HasPointersFlag;
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

        internal uint FieldByteCountNonGCAligned
        {
            get
            {
                // This api is designed to return correct results for EETypes which can be derived from
                // And results indistinguishable from correct for DefTypes which cannot be derived from (sealed classes)
                // (For sealed classes, this should always return BaseSize-((uint)sizeof(ObjHeader));
                Debug.Assert(!IsInterface && !IsParameterizedType);

                // get_BaseSize returns the GC size including space for the sync block index field, the MethodTable* and
                // padding for GC heap alignment. Must subtract all of these to get the size used for the fields of
                // the type (where the fields of the type includes the MethodTable*)
                return BaseSize - ((uint)sizeof(ObjHeader) + ValueTypeFieldPadding);
            }
        }

        internal EEInterfaceInfo* InterfaceMap
        {
            get
            {
                fixed (MethodTable* start = &this)
                {
                    // interface info table starts after the vtable and has _usNumInterfaces entries
                    return (EEInterfaceInfo*)((byte*)start + sizeof(MethodTable) + sizeof(void*) * _usNumVtableSlots);
                }
            }
        }

        internal bool HasDispatchMap
        {
            get
            {
                if (NumInterfaces == 0)
                    return false;
                byte* optionalFields = OptionalFieldsPtr;
                if (optionalFields == null)
                    return false;
                uint idxDispatchMap = OptionalFieldsReader.GetInlineField(optionalFields, EETypeOptionalFieldTag.DispatchMap, 0xffffffff);
                if (idxDispatchMap == 0xffffffff)
                {
                    if (HasDynamicallyAllocatedDispatchMap)
                        return true;
                    else if (IsDynamicType)
                        return DynamicTemplateType->HasDispatchMap;
                    return false;
                }
                return true;
            }
        }

        internal DispatchMap* DispatchMap
        {
            get
            {
                if (NumInterfaces == 0)
                    return null;
                byte* optionalFields = OptionalFieldsPtr;
                if (optionalFields == null)
                    return null;
                uint idxDispatchMap = OptionalFieldsReader.GetInlineField(optionalFields, EETypeOptionalFieldTag.DispatchMap, 0xffffffff);
                if (idxDispatchMap == 0xffffffff && IsDynamicType)
                {
                    if (HasDynamicallyAllocatedDispatchMap)
                    {
                        fixed (MethodTable* pThis = &this)
                            return *(DispatchMap**)((byte*)pThis + GetFieldOffset(EETypeField.ETF_DynamicDispatchMap));
                    }
                    else
                        return DynamicTemplateType->DispatchMap;
                }

                return ((DispatchMap**)TypeManager.DispatchMap)[idxDispatchMap];
            }
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
                if (IsCloned)
                {
                    return CanonicalEEType->BaseType;
                }

                if (IsParameterizedType)
                {
                    if (IsArray)
                        return GetArrayEEType();
                    else
                        return null;
                }

                Debug.Assert(IsCanonical);

                if (IsRelatedTypeViaIAT)
                    return *_relatedType._ppBaseTypeViaIAT;
                else
                    return _relatedType._pBaseType;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType);
                Debug.Assert(!IsParameterizedType);
                Debug.Assert(!IsCloned);
                Debug.Assert(IsCanonical);
                _usFlags &= (ushort)~EETypeFlags.RelatedTypeViaIATFlag;
                _relatedType._pBaseType = value;
            }
#endif
        }

        internal MethodTable* NonArrayBaseType
        {
            get
            {
                Debug.Assert(!IsArray, "array type not supported in BaseType");

                if (IsCloned)
                {
                    // Assuming that since this is not an Array, the CanonicalEEType is also not an array
                    return CanonicalEEType->NonArrayBaseType;
                }

                Debug.Assert(IsCanonical, "we expect canonical types here");

                if (IsRelatedTypeViaIAT)
                {
                    return *_relatedType._ppBaseTypeViaIAT;
                }

                return _relatedType._pBaseType;
            }
        }

        internal MethodTable* NonClonedNonArrayBaseType
        {
            get
            {
                Debug.Assert(!IsArray, "array type not supported in NonArrayBaseType");
                Debug.Assert(!IsCloned, "cloned type not supported in NonClonedNonArrayBaseType");
                Debug.Assert(IsCanonical || IsGenericTypeDefinition, "we expect canonical types here");

                if (IsRelatedTypeViaIAT)
                {
                    return *_relatedType._ppBaseTypeViaIAT;
                }

                return _relatedType._pBaseType;
            }
        }

        internal MethodTable* RawBaseType
        {
            get
            {
                Debug.Assert(!IsParameterizedType, "array type not supported in NonArrayBaseType");
                Debug.Assert(!IsCloned, "cloned type not supported in NonClonedNonArrayBaseType");
                Debug.Assert(IsCanonical, "we expect canonical types here");
                Debug.Assert(!IsRelatedTypeViaIAT, "Non IAT");

                return _relatedType._pBaseType;
            }
        }

        internal MethodTable* CanonicalEEType
        {
            get
            {
                // cloned EETypes must always refer to types in other modules
                Debug.Assert(IsCloned);
                if (IsRelatedTypeViaIAT)
                    return *_relatedType._ppCanonicalTypeViaIAT;
                else
                    return _relatedType._pCanonicalType;
            }
        }

        internal MethodTable* NullableType
        {
            get
            {
                Debug.Assert(IsNullable);
                Debug.Assert(GenericArity == 1);
                return GenericArguments[0].Value;
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

                if (IsRelatedTypeViaIAT)
                    return *_relatedType._ppRelatedParameterTypeViaIAT;
                else
                    return _relatedType._pRelatedParameterType;
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(IsDynamicType && IsParameterizedType);
                _usFlags &= ((ushort)~EETypeFlags.RelatedTypeViaIATFlag);
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

        internal IntPtr GetSealedVirtualSlot(ushort slotNumber)
        {
            Debug.Assert((RareFlags & EETypeRareFlags.HasSealedVTableEntriesFlag) != 0);

            fixed (MethodTable* pThis = &this)
            {
                if (IsDynamicType || !SupportsRelativePointers)
                {
                    uint cbSealedVirtualSlotsTypeOffset = GetFieldOffset(EETypeField.ETF_SealedVirtualSlots);
                    IntPtr* pSealedVirtualsSlotTable = *(IntPtr**)((byte*)pThis + cbSealedVirtualSlotsTypeOffset);
                    return pSealedVirtualsSlotTable[slotNumber];
                }
                else
                {
                    uint cbSealedVirtualSlotsTypeOffset = GetFieldOffset(EETypeField.ETF_SealedVirtualSlots);
                    int* pSealedVirtualsSlotTable = (int*)FollowRelativePointer((int*)((byte*)pThis + cbSealedVirtualSlotsTypeOffset));
                    IntPtr result = FollowRelativePointer(&pSealedVirtualsSlotTable[slotNumber]);
                    return result;
                }
            }
        }

#if TYPE_LOADER_IMPLEMENTATION
        internal void SetSealedVirtualSlot(IntPtr value, ushort slotNumber)
        {
            Debug.Assert(IsDynamicType);

            fixed (MethodTable* pThis = &this)
            {
                uint cbSealedVirtualSlotsTypeOffset = GetFieldOffset(EETypeField.ETF_SealedVirtualSlots);
                IntPtr* pSealedVirtualsSlotTable = *(IntPtr**)((byte*)pThis + cbSealedVirtualSlotsTypeOffset);
                pSealedVirtualsSlotTable[slotNumber] = value;
            }
        }
#endif

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

                _usFlags |= (ushort)EETypeFlags.OptionalFieldsFlag;

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

        internal DynamicModule* DynamicModule
        {
            get
            {
                if ((RareFlags & EETypeRareFlags.HasDynamicModuleFlag) != 0)
                {
                    uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicModule);
                    fixed (MethodTable* pThis = &this)
                    {
                        return *(DynamicModule**)((byte*)pThis + cbOffset);
                    }
                }
                else
                {
                    return null;
                }
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                Debug.Assert(RareFlags.HasFlag(EETypeRareFlags.HasDynamicModuleFlag));
                uint cbOffset = GetFieldOffset(EETypeField.ETF_DynamicModule);
                fixed (MethodTable* pThis = &this)
                {
                    *(DynamicModule**)((byte*)pThis + cbOffset) = value;
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
                uint cbOffset = GetFieldOffset(EETypeField.ETF_TypeManagerIndirection);
                // This is always a pointer to a pointer to a type manager
                return (IntPtr)(*(TypeManagerHandle**)((byte*)Unsafe.AsPointer(ref this) + cbOffset));
            }
            set
            {
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
                return (EETypeElementType)((_usFlags & (ushort)EETypeFlags.ElementTypeMask) >> (ushort)EETypeFlags.ElementTypeShift);
            }
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
            cbOffset += (uint)(sizeof(EEInterfaceInfo) * NumInterfaces);

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

            EETypeRareFlags rareFlags = RareFlags;

            // in the case of sealed vtable entries on static types, we have a UInt sized relative pointer
            if ((rareFlags & EETypeRareFlags.HasSealedVTableEntriesFlag) != 0)
                cbOffset += relativeOrFullPointerOffset;

            if (eField == EETypeField.ETF_DynamicDispatchMap)
            {
                Debug.Assert(IsDynamicType);
                return cbOffset;
            }
            if ((rareFlags & EETypeRareFlags.HasDynamicallyAllocatedDispatchMapFlag) != 0)
                cbOffset += (uint)IntPtr.Size;

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
                Debug.Assert(IsGeneric);
                return cbOffset;
            }
            if (IsGeneric)
            {
                cbOffset += relativeOrFullPointerOffset;
            }

            if (eField == EETypeField.ETF_DynamicModule)
            {
                return cbOffset;
            }

            if ((rareFlags & EETypeRareFlags.HasDynamicModuleFlag) != 0)
                cbOffset += (uint)IntPtr.Size;

            if (eField == EETypeField.ETF_DynamicTemplateType)
            {
                Debug.Assert(IsDynamicType);
                return cbOffset;
            }
            if (IsDynamicType)
                cbOffset += (uint)IntPtr.Size;

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
            bool fHasFinalizer,
            bool fRequiresOptionalFields,
            bool fHasSealedVirtuals,
            bool fHasGenericInfo,
            bool fHasNonGcStatics,
            bool fHasGcStatics,
            bool fHasThreadStatics)
        {
            return (uint)(sizeof(MethodTable) +
                (IntPtr.Size * cVirtuals) +
                (sizeof(EEInterfaceInfo) * cInterfaces) +
                sizeof(IntPtr) + // TypeManager
                (SupportsWritableData ? sizeof(IntPtr) : 0) + // WritableData
                (fHasFinalizer ? sizeof(UIntPtr) : 0) +
                (fRequiresOptionalFields ? sizeof(IntPtr) : 0) +
                (fHasSealedVirtuals ? sizeof(IntPtr) : 0) +
                (fHasGenericInfo ? sizeof(IntPtr)*2 : 0) + // pointers to GenericDefinition and GenericComposition
                (fHasNonGcStatics ? sizeof(IntPtr) : 0) + // pointer to data
                (fHasGcStatics ? sizeof(IntPtr) : 0) +  // pointer to data
                (fHasThreadStatics ? sizeof(uint) : 0)); // tls offset
        }
#endif
    }

    // Wrapper around MethodTable pointers that may be indirected through the IAT if their low bit is set.
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EETypeRef
    {
        private byte* _value;

        public MethodTable* Value
        {
            get
            {
                if (((int)_value & IndirectionConstants.IndirectionCellPointer) == 0)
                    return (MethodTable*)_value;
                return *(MethodTable**)(_value - IndirectionConstants.IndirectionCellPointer);
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _value = (byte*)value;
            }
#endif
        }
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
    internal unsafe readonly struct Pointer<T> where T : unmanaged
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
    internal unsafe readonly struct IatAwarePointer<T> where T : unmanaged
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
    internal unsafe readonly struct RelativePointer<T> where T : unmanaged
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
    internal unsafe readonly struct IatAwareRelativePointer<T> where T : unmanaged
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

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DynamicModule
    {
        // Size field used to indicate the number of bytes of this structure that are defined in Runtime Known ways
        // This is used to drive versioning of this field
        private int _cbSize;

        // Pointer to interface dispatch resolver that works off of a type/slot pair
        // This is a function pointer with the following signature IntPtr()(MethodTable* targetType, MethodTable* interfaceType, ushort slot)
        private delegate*<MethodTable*, MethodTable*, ushort, IntPtr> _dynamicTypeSlotDispatchResolve;

        // Starting address for the the binary module corresponding to this dynamic module.
        private delegate*<ExceptionIDs, Exception> _getRuntimeException;

#if TYPE_LOADER_IMPLEMENTATION
        public int CbSize
        {
            get
            {
                return _cbSize;
            }
            set
            {
                _cbSize = value;
            }
        }
#endif

        public delegate*<MethodTable*, MethodTable*, ushort, IntPtr> DynamicTypeSlotDispatchResolve
        {
            get
            {
                if (_cbSize >= sizeof(IntPtr) * 2)
                {
                    return _dynamicTypeSlotDispatchResolve;
                }
                else
                {
                    return null;
                }
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _dynamicTypeSlotDispatchResolve = value;
            }
#endif
        }

        public delegate*<ExceptionIDs, Exception> GetRuntimeException
        {
            get
            {
                if (_cbSize >= sizeof(IntPtr) * 3)
                {
                    return _getRuntimeException;
                }
                else
                {
                    return null;
                }
            }
#if TYPE_LOADER_IMPLEMENTATION
            set
            {
                _getRuntimeException = value;
            }
#endif
        }

        /////////////////////// END OF FIELDS KNOWN TO THE MRT RUNTIME ////////////////////////
#if TYPE_LOADER_IMPLEMENTATION
        public static readonly int DynamicModuleSize = IntPtr.Size * 3; // We have three fields here.

        // We can put non-low level runtime fields that are module level, that need quick access from a type here
        // For instance, we may choose to put a pointer to the metadata reader or the like here in the future.
#endif
    }
}
