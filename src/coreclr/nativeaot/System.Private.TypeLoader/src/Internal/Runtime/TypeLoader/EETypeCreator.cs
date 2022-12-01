// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;
using System.Collections.Generic;
using System.Threading;

using Internal.Metadata.NativeFormat;
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
        private static IntPtr s_emptyGCDesc;

        private static void CreateEETypeWorker(MethodTable* pTemplateEEType, uint hashCodeOfNewType,
            int arity, TypeBuilderState state)
        {
            bool successful = false;
            IntPtr eeTypePtrPlusGCDesc = IntPtr.Zero;
            IntPtr writableDataPtr = IntPtr.Zero;
            DynamicModule* dynamicModulePtr = null;
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

                ModuleInfo moduleInfo = TypeLoaderEnvironment.GetModuleInfoForType(state.TypeBeingBuilt);
                dynamicModulePtr = moduleInfo.DynamicModulePtr;
                Debug.Assert(dynamicModulePtr != null);

                uint valueTypeFieldPaddingEncoded = 0;
                int baseSize = 0;

                bool isValueType;
                bool hasFinalizer;
                bool isNullable;
                bool isArray;
                bool isGeneric;
                uint flags;
                ushort runtimeInterfacesLength = 0;
                bool isGenericEETypeDef = false;
                bool isAbstractClass;
                bool isByRefLike;
                IntPtr typeManager = IntPtr.Zero;

                if (state.RuntimeInterfaces != null)
                {
                    runtimeInterfacesLength = checked((ushort)state.RuntimeInterfaces.Length);
                }

                if (pTemplateEEType != null)
                {
                    valueTypeFieldPaddingEncoded = EETypeBuilderHelpers.ComputeValueTypeFieldPaddingFieldValue(
                        pTemplateEEType->ValueTypeFieldPadding,
                        (uint)pTemplateEEType->FieldAlignmentRequirement,
                        IntPtr.Size);
                    baseSize = (int)pTemplateEEType->BaseSize;
                    isValueType = pTemplateEEType->IsValueType;
                    hasFinalizer = pTemplateEEType->IsFinalizable;
                    isNullable = pTemplateEEType->IsNullable;
                    flags = pTemplateEEType->Flags;
                    isArray = pTemplateEEType->IsArray;
                    isGeneric = pTemplateEEType->IsGeneric;
                    isAbstractClass = pTemplateEEType->IsAbstract && !pTemplateEEType->IsInterface;
                    isByRefLike = pTemplateEEType->IsByRefLike;
                    typeManager = pTemplateEEType->PointerToTypeManager;
                    Debug.Assert(pTemplateEEType->NumInterfaces == runtimeInterfacesLength);
                }
                else if (state.TypeBeingBuilt.IsGenericDefinition)
                {
                    // is this SUPPORTS_NATIVE_METADATA_TYPE_LOADING?
                    Debug.Assert(false);
                    flags = EETypeBuilderHelpers.ComputeFlags(state.TypeBeingBuilt);
                    Debug.Assert((flags & (uint)EETypeFlags.HasComponentSizeFlag) != 0);
                    flags |= checked((ushort)state.TypeBeingBuilt.Instantiation.Length);

                    isValueType = state.TypeBeingBuilt.IsValueType;
                    hasFinalizer = false;
                    isArray = false;
                    isNullable = false;
                    isGeneric = false;
                    isGenericEETypeDef = true;
                    isAbstractClass = false;
                    isByRefLike = false;
                    baseSize = 0;
                    typeManager = PermanentAllocatedMemoryBlobs.GetPointerToIntPtr(moduleInfo.Handle.GetIntPtrUNSAFE());
                }
                else
                {
                    Debug.Fail("This code path should be unreachable (universal generics).");
                    throw new UnreachableException();
                }

                flags |= (uint)EETypeFlags.IsDynamicTypeFlag;

                // TODO! Change to if template is Universal or non-Existent

                // FEATURE_UNIVERSAL_GENERICS?
                if (state.TypeSize.HasValue)
                {
                    baseSize = state.TypeSize.Value;

                    int baseSizeBeforeAlignment = baseSize;

                    baseSize = MemoryHelpers.AlignUp(baseSize, IntPtr.Size);

                    if (isValueType)
                    {
                        // Compute the valuetype padding size based on size before adding the object type pointer field to the size
                        uint cbValueTypeFieldPadding = (uint)(baseSize - baseSizeBeforeAlignment);

                        // Add Object type pointer field to base size
                        baseSize += IntPtr.Size;

                        valueTypeFieldPaddingEncoded = (uint)EETypeBuilderHelpers.ComputeValueTypeFieldPaddingFieldValue(cbValueTypeFieldPadding, (uint)state.FieldAlignment.Value, IntPtr.Size);
                    }

                    // Minimum base size is 3 pointers, and requires us to bump the size of an empty class type
                    if (baseSize <= IntPtr.Size)
                    {
                        // ValueTypes should already have had their size bumped up by the normal type layout process
                        Debug.Assert(!isValueType);
                        baseSize += IntPtr.Size;
                    }

                    // Add sync block skew
                    baseSize += IntPtr.Size;

                    // Minimum basesize is 3 pointers
                    Debug.Assert(baseSize >= (IntPtr.Size * 3));
                }

                // Optional fields encoding
                int cbOptionalFieldsSize;
                OptionalFieldsRuntimeBuilder optionalFields;
                {
                    optionalFields = new OptionalFieldsRuntimeBuilder(pTemplateEEType != null ? pTemplateEEType->OptionalFieldsPtr : null);

                    uint rareFlags = optionalFields.GetFieldValue(EETypeOptionalFieldTag.RareFlags, 0);

                    if (state.NumSealedVTableEntries > 0)
                        rareFlags |= (uint)EETypeRareFlags.HasSealedVTableEntriesFlag;

                    if (state.NonGcDataSize != 0)
                        rareFlags |= (uint)EETypeRareFlags.IsDynamicTypeWithNonGcStatics;

                    if (state.GcDataSize != 0)
                        rareFlags |= (uint)EETypeRareFlags.IsDynamicTypeWithGcStatics;

                    if (state.ThreadDataSize != 0)
                        rareFlags |= (uint)EETypeRareFlags.IsDynamicTypeWithThreadStatics;

#if TARGET_ARM
                    if (state.FieldAlignment == 8)
                        rareFlags |= (uint)EETypeRareFlags.RequiresAlign8Flag;
                    else
                        rareFlags &= ~(uint)EETypeRareFlags.RequiresAlign8Flag;
#endif

#if TARGET_ARM || TARGET_ARM64
                    if (state.IsHFA)
                        rareFlags |= (uint)EETypeRareFlags.IsHFAFlag;
                    else
                        rareFlags &= ~(uint)EETypeRareFlags.IsHFAFlag;
#endif
                    if (state.HasStaticConstructor)
                        rareFlags |= (uint)EETypeRareFlags.HasCctorFlag;
                    else
                        rareFlags &= ~(uint)EETypeRareFlags.HasCctorFlag;

                    if (isAbstractClass)
                        rareFlags |= (uint)EETypeRareFlags.IsAbstractClassFlag;
                    else
                        rareFlags &= ~(uint)EETypeRareFlags.IsAbstractClassFlag;

                    if (isByRefLike)
                        rareFlags |= (uint)EETypeRareFlags.IsByRefLikeFlag;
                    else
                        rareFlags &= ~(uint)EETypeRareFlags.IsByRefLikeFlag;

                    if (isNullable)
                    {
                        uint nullableValueOffset = state.NullableValueOffset;

                        // The stored offset is never zero (Nullable has a boolean there indicating whether the value is valid).
                        // If the real offset is one, then the field isn't set. Otherwise the offset is encoded - 1 to save space.
                        if (nullableValueOffset == 1)
                            optionalFields.ClearField(EETypeOptionalFieldTag.NullableValueOffset);
                        else
                            optionalFields.SetFieldValue(EETypeOptionalFieldTag.NullableValueOffset, checked(nullableValueOffset - 1));
                    }
                    else
                    {
                        optionalFields.ClearField(EETypeOptionalFieldTag.NullableValueOffset);
                    }

                    rareFlags |= (uint)EETypeRareFlags.HasDynamicModuleFlag;

                    optionalFields.SetFieldValue(EETypeOptionalFieldTag.RareFlags, rareFlags);

                    // Dispatch map is fetched from template type
                    optionalFields.ClearField(EETypeOptionalFieldTag.DispatchMap);

                    optionalFields.ClearField(EETypeOptionalFieldTag.ValueTypeFieldPadding);

                    if (valueTypeFieldPaddingEncoded != 0)
                        optionalFields.SetFieldValue(EETypeOptionalFieldTag.ValueTypeFieldPadding, valueTypeFieldPaddingEncoded);

                    // Compute size of optional fields encoding
                    cbOptionalFieldsSize = optionalFields.Encode();
                    Debug.Assert(cbOptionalFieldsSize > 0);
                }

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
                        true,
                        state.NumSealedVTableEntries > 0,
                        isGeneric,
                        state.NonGcDataSize != 0,
                        state.GcDataSize != 0,
                        state.ThreadDataSize != 0);

                    // Dynamic types have an extra pointer-sized field that contains a pointer to their template type
                    cbEEType += IntPtr.Size;

                    // Add another pointer sized field for a DynamicModule
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
                    pEEType->BaseSize = (uint)baseSize;
                    pEEType->NumVtableSlots = numVtableSlots;
                    pEEType->NumInterfaces = runtimeInterfacesLength;
                    pEEType->HashCode = hashCodeOfNewType;
                    pEEType->PointerToTypeManager = typeManager;

                    // Write the GCDesc
                    bool isSzArray = isArray ? state.ArrayRank < 1 : false;
                    int arrayRank = isArray ? state.ArrayRank.Value : 0;
                    CreateInstanceGCDesc(state, pTemplateEEType, pEEType, baseSize, cbGCDesc, isValueType, isArray, isSzArray, arrayRank);
                    Debug.Assert(pEEType->HasGCPointers == (cbGCDesc != 0));

                    // Copy the encoded optional fields buffer to the newly allocated memory, and update the OptionalFields field on the MethodTable
                    // It is important to set the optional fields first on the newly created MethodTable, because all other 'setters'
                    // will assert that the type is dynamic, just to make sure we are not making any changes to statically compiled types
                    pEEType->OptionalFieldsPtr = (byte*)pEEType + cbEEType;
                    optionalFields.WriteToEEType(pEEType, cbOptionalFieldsSize);

                    pEEType->DynamicModule = dynamicModulePtr;

                    // Copy VTable entries from template type
                    // FEATURE_UNIVERSAL_GENERICS can be simplified to just copy memory
                    int numSlotsFilled = 0;
                    IntPtr* pVtable = (IntPtr*)((byte*)pEEType + sizeof(MethodTable));
                    if (pTemplateEEType != null)
                    {
                        IntPtr* pTemplateVtable = (IntPtr*)((byte*)pTemplateEEType + sizeof(MethodTable));
                        for (int i = 0; i < pTemplateEEType->NumVtableSlots; i++)
                        {
                            int vtableSlotInDynamicType = i;
                            if (vtableSlotInDynamicType != -1)
                            {
                                Debug.Assert(vtableSlotInDynamicType < numVtableSlots);
                                pVtable[vtableSlotInDynamicType] = pTemplateVtable[i];
                                numSlotsFilled++;
                            }
                        }
                    }
                    else if (isGenericEETypeDef)
                    {
                        // If creating a Generic Type Definition
                        Debug.Assert(pEEType->NumVtableSlots == 0);
                    }
                    else
                    {
                        // SUPPORTS_NATIVE_METADATA_TYPE_LOADING restructure the ifs
                        Environment.FailFast("Template type loader is null, but metadata based type loader is not in use");
                    }

                    Debug.Assert(numSlotsFilled == numVtableSlots);

                    // Copy Pointer to finalizer method from the template type
                    if (hasFinalizer)
                    {
                        pEEType->FinalizerCode = pTemplateEEType->FinalizerCode;
                    }
                }

                // Copy the sealed vtable entries if they exist on the template type
                if (state.NumSealedVTableEntries > 0)
                {
                    state.HalfBakedSealedVTable = MemoryHelpers.AllocateMemory((int)state.NumSealedVTableEntries * IntPtr.Size);

                    uint cbSealedVirtualSlotsTypeOffset = pEEType->GetFieldOffset(EETypeField.ETF_SealedVirtualSlots);
                    *((IntPtr*)((byte*)pEEType + cbSealedVirtualSlotsTypeOffset)) = state.HalfBakedSealedVTable;

                    for (ushort i = 0; i < state.NumSealedVTableEntries; i++)
                    {
                        IntPtr value = pTemplateEEType->GetSealedVirtualSlot(i);
                        pEEType->SetSealedVirtualSlot(value, i);
                    }
                }

                if (MethodTable.SupportsWritableData)
                {
                    writableDataPtr = MemoryHelpers.AllocateMemory(WritableData.GetSize(IntPtr.Size));
                    MemoryHelpers.Memset(writableDataPtr, WritableData.GetSize(IntPtr.Size), 0);
                    pEEType->WritableData = writableDataPtr;
                }

                pEEType->DynamicTemplateType = pTemplateEEType;

                int nonGCStaticDataOffset = 0;

                if (!isArray && !isGenericEETypeDef)
                {
                    nonGCStaticDataOffset = state.HasStaticConstructor ? -TypeBuilder.ClassConstructorOffset : 0;

                    // create GC desc
                    if (state.GcDataSize != 0 && state.GcStaticDesc == IntPtr.Zero)
                    {
                        int cbStaticGCDesc;
                        state.GcStaticDesc = CreateStaticGCDesc(state.StaticGCLayout, out state.AllocatedStaticGCDesc, out cbStaticGCDesc);
                    }

                    if (state.ThreadDataSize != 0 && state.ThreadStaticDesc == IntPtr.Zero)
                    {
                        int cbThreadStaticGCDesc;
                        state.ThreadStaticDesc = CreateStaticGCDesc(state.ThreadStaticGCLayout, out state.AllocatedThreadStaticGCDesc, out cbThreadStaticGCDesc);
                    }

                    // If we have a class constructor, our NonGcDataSize MUST be non-zero
                    Debug.Assert(!state.HasStaticConstructor || (state.NonGcDataSize != 0));
                }

                if (isGeneric)
                {
                    genericComposition = MemoryHelpers.AllocateMemory(MethodTable.GetGenericCompositionSize(arity, pEEType->HasGenericVariance));
                    pEEType->SetGenericComposition(genericComposition);

                    if (state.NonGcDataSize > 0)
                    {
                        nonGcStaticData = MemoryHelpers.AllocateMemory(state.NonGcDataSize);
                        MemoryHelpers.Memset(nonGcStaticData, state.NonGcDataSize, 0);
                        Debug.Assert(nonGCStaticDataOffset <= state.NonGcDataSize);
                        pEEType->DynamicNonGcStaticsData = (IntPtr)((byte*)nonGcStaticData + nonGCStaticDataOffset);
                    }
                }

                if (!isGenericEETypeDef && state.ThreadDataSize != 0)
                {
                    state.ThreadStaticOffset = TypeLoaderEnvironment.Instance.GetNextThreadStaticsOffsetValue(pEEType->TypeManager);

                    threadStaticIndex = MemoryHelpers.AllocateMemory(IntPtr.Size * 2);
                    *(IntPtr*)threadStaticIndex = pEEType->PointerToTypeManager;
                    *(((IntPtr*)threadStaticIndex) + 1) = (IntPtr)state.ThreadStaticOffset;
                    pEEType->DynamicThreadStaticsIndex = threadStaticIndex;
                }

                if (!isGenericEETypeDef && state.GcDataSize != 0)
                {
                    // Statics are allocated on GC heap
                    object obj = RuntimeAugments.NewObject(((MethodTable*)state.GcStaticDesc)->ToRuntimeTypeHandle());
                    gcStaticData = RuntimeAugments.RhHandleAlloc(obj, GCHandleType.Normal);

                    pEEType->DynamicGcStaticsData = gcStaticData;
                }

                if (state.Dictionary != null)
                    state.HalfBakedDictionary = state.Dictionary.Allocate();

                Debug.Assert(!state.HalfBakedRuntimeTypeHandle.IsNull());
                Debug.Assert((state.NumSealedVTableEntries == 0 && state.HalfBakedSealedVTable == IntPtr.Zero) || (state.NumSealedVTableEntries > 0 && state.HalfBakedSealedVTable != IntPtr.Zero));
                Debug.Assert((state.Dictionary == null && state.HalfBakedDictionary == IntPtr.Zero) || (state.Dictionary != null && state.HalfBakedDictionary != IntPtr.Zero));

                successful = true;
            }
            finally
            {
                if (!successful)
                {
                    if (eeTypePtrPlusGCDesc != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(eeTypePtrPlusGCDesc);
                    if (state.HalfBakedSealedVTable != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(state.HalfBakedSealedVTable);
                    if (state.HalfBakedDictionary != IntPtr.Zero)
                        MemoryHelpers.FreeMemory(state.HalfBakedDictionary);
                    if (state.AllocatedStaticGCDesc)
                        MemoryHelpers.FreeMemory(state.GcStaticDesc);
                    if (state.AllocatedThreadStaticGCDesc)
                        MemoryHelpers.FreeMemory(state.ThreadStaticDesc);
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

        private static IntPtr CreateStaticGCDesc(LowLevelList<bool> gcBitfield, out bool allocated, out int cbGCDesc)
        {
            if (gcBitfield != null)
            {
                int series = CreateGCDesc(gcBitfield, 0, false, true, null);
                if (series > 0)
                {
                    cbGCDesc = sizeof(int) + series * sizeof(int) * 2;
                    IntPtr result = MemoryHelpers.AllocateMemory(cbGCDesc);
                    CreateGCDesc(gcBitfield, 0, false, true, (void**)result.ToPointer());
                    allocated = true;
                    return result;
                }
            }

            allocated = false;

            if (s_emptyGCDesc == IntPtr.Zero)
            {
                IntPtr ptr = MemoryHelpers.AllocateMemory(8);

                long* gcdesc = (long*)ptr.ToPointer();
                *gcdesc = 0;

                if (Interlocked.CompareExchange(ref s_emptyGCDesc, ptr, IntPtr.Zero) != IntPtr.Zero)
                    MemoryHelpers.FreeMemory(ptr);
            }

            cbGCDesc = IntPtr.Size;
            return s_emptyGCDesc;
        }

        private static void CreateInstanceGCDesc(TypeBuilderState state, MethodTable* pTemplateEEType, MethodTable* pEEType, int baseSize, int cbGCDesc, bool isValueType, bool isArray, bool isSzArray, int arrayRank)
        {
            var gcBitfield = state.InstanceGCLayout;
            if (isArray)
            {
                if (cbGCDesc != 0)
                {
                    pEEType->HasGCPointers = true;
                    if (state.IsArrayOfReferenceTypes)
                    {
                        IntPtr* gcDescStart = (IntPtr*)((byte*)pEEType - cbGCDesc);
                        gcDescStart[0] = new IntPtr(-baseSize);
                        gcDescStart[1] = new IntPtr(baseSize - sizeof(IntPtr));
                        gcDescStart[2] = new IntPtr(1);
                    }
                    else
                    {
                        CreateArrayGCDesc(gcBitfield, arrayRank, isSzArray, ((void**)pEEType) - 1);
                    }
                }
                else
                {
                    pEEType->HasGCPointers = false;
                }
            }
            else if (gcBitfield != null)
            {
                if (cbGCDesc != 0)
                {
                    pEEType->HasGCPointers = true;
                    CreateGCDesc(gcBitfield, baseSize, isValueType, false, ((void**)pEEType) - 1);
                }
                else
                {
                    pEEType->HasGCPointers = false;
                }
            }
            else if (pTemplateEEType != null)
            {
                Buffer.MemoryCopy((byte*)pTemplateEEType - cbGCDesc, (byte*)pEEType - cbGCDesc, cbGCDesc, cbGCDesc);
                pEEType->HasGCPointers = pTemplateEEType->HasGCPointers;
            }
            else
            {
                pEEType->HasGCPointers = false;
            }
        }

        private static unsafe int GetInstanceGCDescSize(TypeBuilderState state, MethodTable* pTemplateEEType, bool isValueType, bool isArray)
        {
            var gcBitfield = state.InstanceGCLayout;
            if (isArray)
            {
                if (state.IsArrayOfReferenceTypes)
                {
                    // Reference type arrays have a GC desc the size of 3 pointers
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
            state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->Flags = EETypeBuilderHelpers.ComputeFlags(byRefType);
            Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ElementType == EETypeElementType.ByRef);
            Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ParameterizedTypeShape == ParameterizedTypeShapeConstants.Pointer);
            state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->ParameterizedTypeShape = ParameterizedTypeShapeConstants.ByRef;

            return state.HalfBakedRuntimeTypeHandle;
        }

        public static RuntimeTypeHandle CreateEEType(TypeDesc type, TypeBuilderState state)
        {
            Debug.Assert(type != null && state != null);

            MethodTable* pTemplateEEType;

            if (type is PointerType || type is ByRefType)
            {
                Debug.Assert(0 == state.NonGcDataSize);
                Debug.Assert(false == state.HasStaticConstructor);
                Debug.Assert(0 == state.GcDataSize);
                Debug.Assert(0 == state.ThreadStaticOffset);
                Debug.Assert(0 == state.NumSealedVTableEntries);
                Debug.Assert(IntPtr.Zero == state.GcStaticDesc);
                Debug.Assert(IntPtr.Zero == state.ThreadStaticDesc);

                // Pointers and ByRefs only differ by the ParameterizedTypeShape and ElementType value.
                RuntimeTypeHandle templateTypeHandle = typeof(void*).TypeHandle;

                pTemplateEEType = templateTypeHandle.ToEETypePtr();
            }
            else if (type.IsMdArray || (type.IsSzArray && ((ArrayType)type).ElementType.IsPointer))
            {
                // Multidimensional arrays and szarrays of pointers don't implement generic interfaces and
                // we don't need to do much for them in terms of type building. We can pretty much just take
                // the MethodTable for any of those, massage the bits that matter (GCDesc, element type,
                // component size,...) to be of the right shape and we're done.
                pTemplateEEType = typeof(object[,]).TypeHandle.ToEETypePtr();
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
