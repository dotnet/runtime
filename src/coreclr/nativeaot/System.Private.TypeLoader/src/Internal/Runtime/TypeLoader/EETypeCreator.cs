// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Runtime.Augments;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    internal static class RuntimeTypeHandleEETypeExtensions
    {
        public static unsafe MethodTable* ToEETypePtr(this RuntimeTypeHandle rtth)
        {
            return (MethodTable*)(*(IntPtr*)&rtth);
        }

        public static unsafe IntPtr ToIntPtr(this RuntimeTypeHandle rtth)
        {
            return *(IntPtr*)&rtth;
        }

        public static unsafe bool IsDynamicType(this RuntimeTypeHandle rtth)
        {
            return rtth.ToEETypePtr()->IsDynamicType;
        }

        public static unsafe int GetNumVtableSlots(this RuntimeTypeHandle rtth)
        {
            return rtth.ToEETypePtr()->NumVtableSlots;
        }

        public static unsafe TypeManagerHandle GetTypeManager(this RuntimeTypeHandle rtth)
        {
            return rtth.ToEETypePtr()->TypeManager;
        }

        public static unsafe IntPtr GetDictionary(this RuntimeTypeHandle rtth)
        {
            return EETypeCreator.GetDictionary(rtth.ToEETypePtr());
        }

        public static unsafe void SetDictionary(this RuntimeTypeHandle rtth, int dictionarySlot, IntPtr dictionary)
        {
            Debug.Assert(rtth.ToEETypePtr()->IsDynamicType && dictionarySlot < rtth.GetNumVtableSlots());
            *(IntPtr*)((byte*)rtth.ToEETypePtr() + sizeof(MethodTable) + dictionarySlot * IntPtr.Size) = dictionary;
        }

        public static unsafe void SetInterface(this RuntimeTypeHandle rtth, int interfaceIndex, RuntimeTypeHandle interfaceType)
        {
            rtth.ToEETypePtr()->InterfaceMap[interfaceIndex].InterfaceType = interfaceType.ToEETypePtr();
        }

        public static unsafe void SetGenericDefinition(this RuntimeTypeHandle rtth, RuntimeTypeHandle genericDefinitionHandle)
        {
            rtth.ToEETypePtr()->GenericDefinition = genericDefinitionHandle.ToEETypePtr();
        }

        public static unsafe void SetGenericVariance(this RuntimeTypeHandle rtth, int argumentIndex, GenericVariance variance)
        {
            rtth.ToEETypePtr()->GenericVariance[argumentIndex] = variance;
        }

        public static unsafe void SetGenericArity(this RuntimeTypeHandle rtth, uint arity)
        {
            rtth.ToEETypePtr()->GenericArity = arity;
        }

        public static unsafe void SetGenericArgument(this RuntimeTypeHandle rtth, int argumentIndex, RuntimeTypeHandle argumentType)
        {
            rtth.ToEETypePtr()->GenericArguments[argumentIndex].Value = argumentType.ToEETypePtr();
        }

        public static unsafe void SetRelatedParameterType(this RuntimeTypeHandle rtth, RuntimeTypeHandle relatedTypeHandle)
        {
            rtth.ToEETypePtr()->RelatedParameterType = relatedTypeHandle.ToEETypePtr();
        }

        public static unsafe void SetParameterizedTypeShape(this RuntimeTypeHandle rtth, uint value)
        {
            rtth.ToEETypePtr()->ParameterizedTypeShape = value;
        }

        public static unsafe void SetBaseType(this RuntimeTypeHandle rtth, RuntimeTypeHandle baseTypeHandle)
        {
            rtth.ToEETypePtr()->BaseType = baseTypeHandle.ToEETypePtr();
        }

        public static unsafe void SetComponentSize(this RuntimeTypeHandle rtth, ushort componentSize)
        {
            Debug.Assert(componentSize > 0);
            Debug.Assert(rtth.ToEETypePtr()->IsArray || rtth.ToEETypePtr()->IsString);
            rtth.ToEETypePtr()->HasComponentSize = true;
            rtth.ToEETypePtr()->ComponentSize = componentSize;
        }
    }

    internal static class MemoryHelpers
    {
        public static int AlignUp(int val, int alignment)
        {
            Debug.Assert(val >= 0 && alignment >= 0);

            // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
            Debug.Assert(0 == (alignment & (alignment - 1)));
            int result = (val + (alignment - 1)) & ~(alignment - 1);
            Debug.Assert(result >= val);      // check for overflow

            return result;
        }

        public static unsafe void Memset(IntPtr destination, int length, byte value)
        {
            byte* pbDest = (byte*)destination.ToPointer();
            while (length > 0)
            {
                *pbDest = value;
                pbDest++;
                length--;
            }
        }

        public static unsafe IntPtr AllocateMemory(int cbBytes)
        {
            return (IntPtr)NativeMemory.Alloc((nuint)cbBytes);
        }

        public static unsafe void FreeMemory(IntPtr memoryPtrToFree)
        {
            NativeMemory.Free((void*)memoryPtrToFree);
        }
    }

    internal static unsafe class EETypeCreator
    {
        private static void CreateEETypeWorker(MethodTable* pTemplateEEType, uint hashCodeOfNewType,
            int arity, TypeBuilderState state)
        {
            bool successful = false;
            IntPtr eeTypePtrPlusGCDesc = IntPtr.Zero;
            IntPtr writableDataPtr = IntPtr.Zero;
            IntPtr gcStaticData = IntPtr.Zero;
            IntPtr nonGcStaticData = IntPtr.Zero;
            IntPtr genericComposition = IntPtr.Zero;
            IntPtr threadStaticIndex = IntPtr.Zero;

            try
            {
                Debug.Assert(pTemplateEEType != null);

                // In some situations involving arrays we can find as a template a dynamically generated type.
                // In that case, the correct template would be the template used to create the dynamic type in the first
                // place.
                if (pTemplateEEType->IsDynamicType)
                {
                    pTemplateEEType = pTemplateEEType->DynamicTemplateType;
                }

                int baseSize = 0;

                bool isValueType;
                bool hasFinalizer;
                bool isNullable;
                bool isArray;
                bool isGeneric;
                uint flags;
                ushort runtimeInterfacesLength = 0;
                IntPtr typeManager = IntPtr.Zero;

                if (state.RuntimeInterfaces != null)
                {
                    runtimeInterfacesLength = checked((ushort)state.RuntimeInterfaces.Length);
                }

                baseSize = (int)pTemplateEEType->RawBaseSize;
                isValueType = pTemplateEEType->IsValueType;
                hasFinalizer = pTemplateEEType->IsFinalizable;
                isNullable = pTemplateEEType->IsNullable;
                flags = pTemplateEEType->Flags;
                isArray = pTemplateEEType->IsArray;
                isGeneric = pTemplateEEType->IsGeneric;
                typeManager = pTemplateEEType->PointerToTypeManager;
                Debug.Assert(pTemplateEEType->NumInterfaces == runtimeInterfacesLength);

                flags |= (uint)EETypeFlags.IsDynamicTypeFlag;

                int numFunctionPointerTypeParameters = 0;
                if (state.TypeBeingBuilt.IsMdArray)
                {
                    // If we're building an MDArray, the template is object[,] and we
                    // need to recompute the base size.
                    baseSize = IntPtr.Size + // sync block
                        2 * IntPtr.Size + // EETypePtr + Length
                        state.ArrayRank.Value * sizeof(int) * 2; // 2 ints per rank for bounds
                }
                else if (state.TypeBeingBuilt.IsFunctionPointer)
                {
                    // Base size encodes number of parameters and calling convention
                    MethodSignature sig = ((FunctionPointerType)state.TypeBeingBuilt).Signature;
                    baseSize = (sig.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) switch
                    {
                        0 => sig.Length,
                        _ => sig.Length | unchecked((int)FunctionPointerFlags.IsUnmanaged),
                    };
                    numFunctionPointerTypeParameters = sig.Length;
                }

                // Optional fields encoding
                int cbOptionalFieldsSize;
                OptionalFieldsRuntimeBuilder optionalFields = new OptionalFieldsRuntimeBuilder(pTemplateEEType->OptionalFieldsPtr);

                uint rareFlags = optionalFields.GetFieldValue(EETypeOptionalFieldTag.RareFlags, 0);

                int allocatedNonGCDataSize = state.NonGcDataSize;
                if (state.HasStaticConstructor)
                    allocatedNonGCDataSize += -TypeBuilder.ClassConstructorOffset;

                if (allocatedNonGCDataSize != 0)
                    rareFlags |= (uint)EETypeRareFlags.IsDynamicTypeWithNonGcStatics;

                if (state.GcDataSize != 0)
                    rareFlags |= (uint)EETypeRareFlags.IsDynamicTypeWithGcStatics;

                if (state.ThreadDataSize != 0)
                    rareFlags |= (uint)EETypeRareFlags.IsDynamicTypeWithThreadStatics;

                if (rareFlags != 0)
                    optionalFields.SetFieldValue(EETypeOptionalFieldTag.RareFlags, rareFlags);

                // Dispatch map is fetched from template type
                optionalFields.ClearField(EETypeOptionalFieldTag.DispatchMap);

                // Compute size of optional fields encoding
                cbOptionalFieldsSize = optionalFields.Encode();

                // Clear the optional fields flag. We'll set it if we set optional fields later in this method.
                if (cbOptionalFieldsSize == 0)
                    flags &= ~(uint)EETypeFlags.OptionalFieldsFlag;

                // Note: The number of vtable slots on the MethodTable to create is not necessary equal to the number of
                // vtable slots on the template type for universal generics (see ComputeVTableLayout)
                ushort numVtableSlots = state.NumVTableSlots;

                // Compute the MethodTable size and allocate it
                MethodTable* pEEType;
                {
                    // In order to get the size of the MethodTable to allocate we need the following information
                    // 1) The number of VTable slots (from the TypeBuilderState)
                    // 2) The number of Interfaces (from the template)
                    // 3) Whether or not there is a finalizer (from the template)
                    // 4) Optional fields size
                    // 5) Whether or not the type has sealed virtuals (from the TypeBuilderState)
                    int cbEEType = (int)MethodTable.GetSizeofEEType(
                        numVtableSlots,
                        runtimeInterfacesLength,
                        hasFinalizer,
                        cbOptionalFieldsSize > 0,
                        (rareFlags & (int)EETypeRareFlags.HasSealedVTableEntriesFlag) != 0,
                        isGeneric,
                        numFunctionPointerTypeParameters,
                        allocatedNonGCDataSize != 0,
                        state.GcDataSize != 0,
                        state.ThreadDataSize != 0);

                    // Dynamic types have an extra pointer-sized field that contains a pointer to their template type
                    cbEEType += IntPtr.Size;

                    int cbGCDesc = GetInstanceGCDescSize(state, pTemplateEEType, isValueType, isArray);
                    int cbGCDescAligned = MemoryHelpers.AlignUp(cbGCDesc, IntPtr.Size);

                    // Allocate enough space for the MethodTable + gcDescSize
                    eeTypePtrPlusGCDesc = MemoryHelpers.AllocateMemory(cbGCDescAligned + cbEEType + cbOptionalFieldsSize);

                    // Get the MethodTable pointer, and the template MethodTable pointer
                    pEEType = (MethodTable*)(eeTypePtrPlusGCDesc + cbGCDescAligned);
                    state.HalfBakedRuntimeTypeHandle = pEEType->ToRuntimeTypeHandle();

                    // Set basic MethodTable fields
                    pEEType->Flags = flags;
                    pEEType->RawBaseSize = (uint)baseSize;
                    pEEType->NumVtableSlots = numVtableSlots;
                    pEEType->NumInterfaces = runtimeInterfacesLength;
                    pEEType->HashCode = hashCodeOfNewType;
                    pEEType->PointerToTypeManager = typeManager;

                    // Write the GCDesc
                    bool isSzArray = isArray ? state.ArrayRank < 1 : false;
                    int arrayRank = isArray ? state.ArrayRank.Value : 0;
                    CreateInstanceGCDesc(state, pTemplateEEType, pEEType, baseSize, cbGCDesc, isValueType, isArray, isSzArray, arrayRank);
                    Debug.Assert(pEEType->ContainsGCPointers == (cbGCDesc != 0));

                    // Copy the encoded optional fields buffer to the newly allocated memory, and update the OptionalFields field on the MethodTable
                    if (cbOptionalFieldsSize > 0)
                    {
                        pEEType->OptionalFieldsPtr = (byte*)pEEType + cbEEType;
                        optionalFields.WriteToEEType(pEEType, cbOptionalFieldsSize);
                    }

                    // Copy VTable entries from template type
                    IntPtr* pVtable = (IntPtr*)((byte*)pEEType + sizeof(MethodTable));
                    IntPtr* pTemplateVtable = (IntPtr*)((byte*)pTemplateEEType + sizeof(MethodTable));
                    for (int i = 0; i < numVtableSlots; i++)
                        pVtable[i] = pTemplateVtable[i];

                    // Copy Pointer to finalizer method from the template type
                    if (hasFinalizer)
                    {
                        pEEType->FinalizerCode = pTemplateEEType->FinalizerCode;
                    }
                }

                // Copy the sealed vtable entries if they exist on the template type
                if ((rareFlags & (int)EETypeRareFlags.HasSealedVTableEntriesFlag) != 0)
                {
                    uint cbSealedVirtualSlotsTypeOffset = pEEType->GetFieldOffset(EETypeField.ETF_SealedVirtualSlots);
                    *((void**)((byte*)pEEType + cbSealedVirtualSlotsTypeOffset)) = pTemplateEEType->GetSealedVirtualTable();
                }

                if (MethodTable.SupportsWritableData)
                {
                    writableDataPtr = MemoryHelpers.AllocateMemory(WritableData.GetSize(IntPtr.Size));
                    MemoryHelpers.Memset(writableDataPtr, WritableData.GetSize(IntPtr.Size), 0);
                    pEEType->WritableData = writableDataPtr;
                }

                pEEType->DynamicTemplateType = pTemplateEEType;

                int nonGCStaticDataOffset = 0;

                if (!isArray)
                {
                    nonGCStaticDataOffset = state.HasStaticConstructor ? -TypeBuilder.ClassConstructorOffset : 0;

                    // If we have a class constructor, our NonGcDataSize MUST be non-zero
                    Debug.Assert(!state.HasStaticConstructor || (allocatedNonGCDataSize != 0));
                }

                if (isGeneric)
                {
                    genericComposition = MemoryHelpers.AllocateMemory(MethodTable.GetGenericCompositionSize(arity, pEEType->HasGenericVariance));
                    pEEType->SetGenericComposition(genericComposition);

                    if (allocatedNonGCDataSize > 0)
                    {
                        nonGcStaticData = MemoryHelpers.AllocateMemory(allocatedNonGCDataSize);
                        MemoryHelpers.Memset(nonGcStaticData, allocatedNonGCDataSize, 0);
                        Debug.Assert(nonGCStaticDataOffset <= allocatedNonGCDataSize);
                        pEEType->DynamicNonGcStaticsData = (IntPtr)((byte*)nonGcStaticData + nonGCStaticDataOffset);
                    }
                }

                if (state.ThreadDataSize != 0)
                {
                    state.ThreadStaticOffset = TypeLoaderEnvironment.Instance.GetNextThreadStaticsOffsetValue(pEEType->TypeManager);

                    threadStaticIndex = MemoryHelpers.AllocateMemory(IntPtr.Size * 2);
                    *(IntPtr*)threadStaticIndex = pEEType->PointerToTypeManager;
                    *(((IntPtr*)threadStaticIndex) + 1) = (IntPtr)state.ThreadStaticOffset;
                    pEEType->DynamicThreadStaticsIndex = threadStaticIndex;
                }

                if (state.GcDataSize != 0)
                {
                    // Statics are allocated on GC heap
                    object obj = RuntimeAugments.NewObject(((MethodTable*)state.GcStaticDesc)->ToRuntimeTypeHandle());
                    gcStaticData = RuntimeAugments.RhHandleAlloc(obj, GCHandleType.Normal);

                    pEEType->DynamicGcStaticsData = gcStaticData;
                }

                if (state.Dictionary != null)
                    state.HalfBakedDictionary = state.Dictionary.Allocate();

                Debug.Assert(!state.HalfBakedRuntimeTypeHandle.IsNull());
                Debug.Assert((state.Dictionary == null && state.HalfBakedDictionary == IntPtr.Zero) || (state.Dictionary != null && state.HalfBakedDictionary != IntPtr.Zero));

                successful = true;
            }
            finally
            {
                if (!successful)
                {
                    if (eeTypePtrPlusGCDesc != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(eeTypePtrPlusGCDesc);
                    if (state.HalfBakedDictionary != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(state.HalfBakedDictionary);
                    if (gcStaticData != IntPtr.Zero)
                        RuntimeAugments.RhHandleFree(gcStaticData);
                    if (genericComposition != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(genericComposition);
                    if (nonGcStaticData != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(nonGcStaticData);
                    if (writableDataPtr != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(writableDataPtr);
                    if (threadStaticIndex != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(threadStaticIndex);
                }
            }
        }

        private static void CreateInstanceGCDesc(TypeBuilderState state, MethodTable* pTemplateEEType, MethodTable* pEEType, int baseSize, int cbGCDesc, bool isValueType, bool isArray, bool isSzArray, int arrayRank)
        {
            var gcBitfield = state.InstanceGCLayout;
            if (isArray)
            {
                if (cbGCDesc != 0)
                {
                    pEEType->ContainsGCPointers = true;
                    if (state.IsArrayOfReferenceTypes || IsAllGCPointers(gcBitfield))
                    {
                        IntPtr* gcDescStart = (IntPtr*)((byte*)pEEType - cbGCDesc);
                        // Series size
                        gcDescStart[0] = new IntPtr(-baseSize);
                        // Series offset
                        gcDescStart[1] = new IntPtr(baseSize - sizeof(IntPtr));
                        // NumSeries
                        gcDescStart[2] = new IntPtr(1);
                    }
                    else
                    {
                        CreateArrayGCDesc(gcBitfield, arrayRank, isSzArray, ((void**)pEEType) - 1);
                    }
                }
                else
                {
                    pEEType->ContainsGCPointers = false;
                }
            }
            else if (gcBitfield != null)
            {
                if (cbGCDesc != 0)
                {
                    pEEType->ContainsGCPointers = true;
                    CreateGCDesc(gcBitfield, baseSize, isValueType, false, ((void**)pEEType) - 1);
                }
                else
                {
                    pEEType->ContainsGCPointers = false;
                }
            }
            else if (pTemplateEEType != null)
            {
                Buffer.MemoryCopy((byte*)pTemplateEEType - cbGCDesc, (byte*)pEEType - cbGCDesc, cbGCDesc, cbGCDesc);
                pEEType->ContainsGCPointers = pTemplateEEType->ContainsGCPointers;
            }
            else
            {
                pEEType->ContainsGCPointers = false;
            }
        }

        private static unsafe int GetInstanceGCDescSize(TypeBuilderState state, MethodTable* pTemplateEEType, bool isValueType, bool isArray)
        {
            var gcBitfield = state.InstanceGCLayout;
            if (isArray)
            {
                if (state.IsArrayOfReferenceTypes ||
                    (gcBitfield != null && IsAllGCPointers(gcBitfield)))
                {
                    // For efficiency this is special cased and encoded as one serie
                    return 3 * sizeof(IntPtr);
                }
                else
                {
                    int series = 0;
                    if (gcBitfield != null)
                        series = CreateArrayGCDesc(gcBitfield, 1, true, null);

                    return series > 0 ? (series + 2) * IntPtr.Size : 0;
                }
            }
            else if (gcBitfield != null)
            {
                int series = CreateGCDesc(gcBitfield, 0, isValueType, false, null);
                return series > 0 ? (series * 2 + 1) * IntPtr.Size : 0;
            }
            else if (pTemplateEEType != null)
            {
                return RuntimeAugments.GetGCDescSize(pTemplateEEType->ToRuntimeTypeHandle());
            }
            else
            {
                return 0;
            }
        }

        private static bool IsAllGCPointers(LowLevelList<bool> bitfield)
        {
            int count = bitfield.Count;
            Debug.Assert(count > 0);

            for (int i = 0; i < count; i++)
            {
                if (!bitfield[i])
                    return false;
            }

            return true;
        }

        private static unsafe int CreateArrayGCDesc(LowLevelList<bool> bitfield, int rank, bool isSzArray, void* gcdesc)
        {
            if (bitfield == null)
                return 0;

            void** baseOffsetPtr = (void**)gcdesc - 1;

#if TARGET_64BIT
            int* ptr = (int*)baseOffsetPtr - 1;
#else
            short* ptr = (short*)baseOffsetPtr - 1;
#endif
            int baseOffset = 2;
            if (!isSzArray)
            {
                baseOffset += 2 * rank / (sizeof(IntPtr) / sizeof(int));
            }

            int numSeries = 0;
            int i = 0;

            bool first = true;
            int last = 0;
            short numPtrs = 0;
            while (i < bitfield.Count)
            {
                if (bitfield[i])
                {
                    if (first)
                    {
                        baseOffset += i;
                        first = false;
                    }
                    else if (gcdesc != null)
                    {
                        *ptr-- = (short)((i - last) * IntPtr.Size);
                        *ptr-- = numPtrs;
                    }

                    numSeries++;
                    numPtrs = 0;

                    while ((i < bitfield.Count) && (bitfield[i]))
                    {
                        numPtrs++;
                        i++;
                    }

                    last = i;
                }
                else
                {
                    i++;
                }
            }

            if (gcdesc != null)
            {
                if (numSeries > 0)
                {
                    *ptr-- = (short)((bitfield.Count - last + baseOffset - 2) * IntPtr.Size);
                    *ptr-- = numPtrs;

                    *(void**)gcdesc = (void*)-numSeries;
                    *baseOffsetPtr = (void*)(baseOffset * IntPtr.Size);
                }
            }

            return numSeries;
        }

        private static unsafe int CreateGCDesc(LowLevelList<bool> bitfield, int size, bool isValueType, bool isStatic, void* gcdesc)
        {
            int offs = 0;
            // if this type is a class we have to account for the gcdesc.
            if (isValueType)
                offs = IntPtr.Size;

            if (bitfield == null)
                return 0;

            void** ptr = (void**)gcdesc - 1;

            int* staticPtr = isStatic ? ((int*)gcdesc + 1) : null;

            int numSeries = 0;
            int i = 0;
            while (i < bitfield.Count)
            {
                if (bitfield[i])
                {
                    numSeries++;
                    int seriesOffset = i * IntPtr.Size + offs;
                    int seriesSize = 0;

                    while ((i < bitfield.Count) && (bitfield[i]))
                    {
                        seriesSize += IntPtr.Size;
                        i++;
                    }


                    if (gcdesc != null)
                    {
                        if (staticPtr != null)
                        {
                            *staticPtr++ = seriesSize;
                            *staticPtr++ = seriesOffset;
                        }
                        else
                        {
                            seriesSize -= size;
                            *ptr-- = (void*)seriesOffset;
                            *ptr-- = (void*)seriesSize;
                        }
                    }
                }
                else
                {
                    i++;
                }
            }

            if (gcdesc != null)
            {
                if (staticPtr != null)
                    *(int*)gcdesc = numSeries;
                else
                    *(void**)gcdesc = (void*)numSeries;
            }

            return numSeries;
        }

        public static RuntimeTypeHandle CreatePointerEEType(uint hashCodeOfNewType, RuntimeTypeHandle pointeeTypeHandle, TypeDesc pointerType)
        {
            TypeBuilderState state = new TypeBuilderState(pointerType);

            CreateEETypeWorker(typeof(void*).TypeHandle.ToEETypePtr(), hashCodeOfNewType, 0, state);
            Debug.Assert(!state.HalfBakedRuntimeTypeHandle.IsNull());

            TypeLoaderLogger.WriteLine("Allocated new POINTER type " + pointerType.ToString() + " with hashcode value = 0x" + hashCodeOfNewType.LowLevelToString() + " with MethodTable = " + state.HalfBakedRuntimeTypeHandle.ToIntPtr().LowLevelToString());

            state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->RelatedParameterType = pointeeTypeHandle.ToEETypePtr();

            return state.HalfBakedRuntimeTypeHandle;
        }

        public static RuntimeTypeHandle CreateByRefEEType(uint hashCodeOfNewType, RuntimeTypeHandle pointeeTypeHandle, TypeDesc byRefType)
        {
            TypeBuilderState state = new TypeBuilderState(byRefType);

            // ByRef and pointer types look similar enough that we can use void* as a template.
            // Ideally this should be typeof(void&) but C# doesn't support that syntax. We adjust for this below.
            CreateEETypeWorker(typeof(void*).TypeHandle.ToEETypePtr(), hashCodeOfNewType, 0, state);
            Debug.Assert(!state.HalfBakedRuntimeTypeHandle.IsNull());

            TypeLoaderLogger.WriteLine("Allocated new BYREF type " + byRefType.ToString() + " with hashcode value = 0x" + hashCodeOfNewType.LowLevelToString() + " with MethodTable = " + state.HalfBakedRuntimeTypeHandle.ToIntPtr().LowLevelToString());

            state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->RelatedParameterType = pointeeTypeHandle.ToEETypePtr();

            // We used a pointer as a template. We need to make this a byref.
            Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ElementType == EETypeElementType.Pointer);
            state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ElementType = EETypeElementType.ByRef;
            Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ParameterizedTypeShape == ParameterizedTypeShapeConstants.Pointer);
            state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ParameterizedTypeShape = ParameterizedTypeShapeConstants.ByRef;

            return state.HalfBakedRuntimeTypeHandle;
        }

        public static RuntimeTypeHandle CreateEEType(TypeDesc type, TypeBuilderState state)
        {
            Debug.Assert(type != null && state != null);

            MethodTable* pTemplateEEType;

            if (type is PointerType || type is ByRefType || type is FunctionPointerType)
            {
                Debug.Assert(0 == state.NonGcDataSize);
                Debug.Assert(false == state.HasStaticConstructor);
                Debug.Assert(0 == state.GcDataSize);
                Debug.Assert(0 == state.ThreadStaticOffset);
                Debug.Assert(IntPtr.Zero == state.GcStaticDesc);
                Debug.Assert(IntPtr.Zero == state.ThreadStaticDesc);

                RuntimeTypeHandle templateTypeHandle;
                if (type is FunctionPointerType)
                {
                    // There's still differences to paper over, but `delegate*<void>` is close enough.
                    templateTypeHandle = typeof(delegate*<void>).TypeHandle;
                }
                else
                {
                    // Pointers and ByRefs only differ by the ParameterizedTypeShape and ElementType value.
                    templateTypeHandle = typeof(void*).TypeHandle;
                }

                pTemplateEEType = templateTypeHandle.ToEETypePtr();
            }
            else
            {
                Debug.Assert(state.TemplateType != null && !state.TemplateType.RuntimeTypeHandle.IsNull());
                RuntimeTypeHandle templateTypeHandle = state.TemplateType.RuntimeTypeHandle;
                pTemplateEEType = templateTypeHandle.ToEETypePtr();
            }

            DefType typeAsDefType = type as DefType;
            // Use a checked typecast to 'ushort' for the arity to ensure its value never exceeds 65535 and cause integer
            // overflows later when computing size of memory blocks to allocate for the type and its GenericInstanceDescriptor structures
            int arity = checked((ushort)((typeAsDefType != null && typeAsDefType.HasInstantiation ? typeAsDefType.Instantiation.Length : 0)));

            CreateEETypeWorker(pTemplateEEType, (uint)type.GetHashCode(), arity, state);

            return state.HalfBakedRuntimeTypeHandle;
        }

        public static int GetDictionaryOffsetInEEtype(MethodTable* pEEType)
        {
            // Dictionary slot is the first vtable slot

            MethodTable* pBaseType = pEEType->BaseType;
            int dictionarySlot = (pBaseType == null ? 0 : pBaseType->NumVtableSlots);
            return sizeof(MethodTable) + dictionarySlot * IntPtr.Size;
        }

        public static IntPtr GetDictionaryAtOffset(MethodTable* pEEType, int offset)
        {
            return *(IntPtr*)((byte*)pEEType + offset);
        }

        public static IntPtr GetDictionary(MethodTable* pEEType)
        {
            return GetDictionaryAtOffset(pEEType, GetDictionaryOffsetInEEtype(pEEType));
        }

        public static int GetDictionarySlotInVTable(TypeDesc type)
        {
            if (!type.CanShareNormalGenericCode())
                return -1;

            // Dictionary slot is the first slot in the vtable after the base type's vtable entries
            return type.BaseType != null ? type.BaseType.GetOrCreateTypeBuilderState().NumVTableSlots : 0;
        }
    }
}
