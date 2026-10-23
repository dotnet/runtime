// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Runtime.Augments;
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

        public static unsafe bool IsDynamicTypeWithCctor(this RuntimeTypeHandle rtth)
        {
            return rtth.ToEETypePtr()->IsDynamicTypeWithCctor;
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
            rtth.ToEETypePtr()->InterfaceMap[interfaceIndex] = interfaceType.ToEETypePtr();
        }

        public static unsafe void SetGenericDefinition(this RuntimeTypeHandle rtth, RuntimeTypeHandle genericDefinitionHandle)
        {
            rtth.ToEETypePtr()->GenericDefinition = genericDefinitionHandle.ToEETypePtr();
        }

        public static unsafe void SetGenericArgument(this RuntimeTypeHandle rtth, int argumentIndex, RuntimeTypeHandle argumentType)
        {
            MethodTableList argumentList = rtth.ToEETypePtr()->GenericArguments;
            argumentList[argumentIndex] = argumentType.ToEETypePtr();
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

        public static unsafe void* AllocateMemory(int cbBytes)
        {
            return NativeMemory.Alloc((nuint)cbBytes);
        }

        public static unsafe void FreeMemory(void* memoryPtrToFree)
        {
            NativeMemory.Free(memoryPtrToFree);
        }
    }

    internal static unsafe class EETypeCreator
    {
        private static void CreateEETypeWorker(MethodTable* pTemplateEEType, uint hashCodeOfNewType,
            int arity, TypeBuilderState state)
        {
            bool successful = false;
            void* eeTypePlusGCDesc = null;
            void* writableData = null;
            void* nonGcStaticData = null;
            void* genericComposition = null;
            void* threadStaticIndex = null;
            nint gcStaticData = 0;

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

                int baseSize = (int)pTemplateEEType->RawBaseSize;
                bool hasFinalizer = pTemplateEEType->IsFinalizable;
                bool hasDispatchMap = pTemplateEEType->HasDispatchMap;
                bool isGeneric = pTemplateEEType->IsGeneric;
                bool hasSealedVTable = pTemplateEEType->HasSealedVTableEntries;
                ushort runtimeInterfacesLength = pTemplateEEType->NumInterfaces;
                Debug.Assert(runtimeInterfacesLength == state.TypeBeingBuilt.RuntimeInterfaces.Length);
                uint flags = pTemplateEEType->Flags | (uint)EETypeFlags.IsDynamicTypeFlag;
                bool isMdArray = state.TypeBeingBuilt.IsMdArray;

                int numFunctionPointerTypeParameters = 0;
                if (isMdArray)
                {
                    // If we're building an MDArray, the template is object[,] and we
                    // need to recompute the base size.
                    baseSize = IntPtr.Size + // sync block
                        2 * IntPtr.Size + // EETypePtr + Length
                        ((ArrayType)state.TypeBeingBuilt).Rank * sizeof(int) * 2; // 2 ints per rank for bounds
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

                DynamicTypeFlags dynamicTypeFlags = 0;

                int allocatedNonGCDataSize = state.NonGcDataSize;
                if (state.HasStaticConstructor)
                {
                    allocatedNonGCDataSize += -TypeBuilder.ClassConstructorOffset;
                    dynamicTypeFlags |= DynamicTypeFlags.HasLazyCctor;
                }

                if (allocatedNonGCDataSize != 0)
                    dynamicTypeFlags |= DynamicTypeFlags.HasNonGCStatics;

                if (state.GcStaticDesc != IntPtr.Zero)
                    dynamicTypeFlags |= DynamicTypeFlags.HasGCStatics;

                if (state.ThreadStaticDesc != IntPtr.Zero)
                    dynamicTypeFlags |= DynamicTypeFlags.HasThreadStatics;

                ushort numVtableSlots = pTemplateEEType->NumVtableSlots;

                // Compute the MethodTable size and allocate it
                MethodTable* pEEType;
                {
                    int cbEEType = (int)MethodTable.GetSizeofEEType(
                        numVtableSlots,
                        runtimeInterfacesLength,
                        hasDispatchMap,
                        hasFinalizer,
                        hasSealedVTable,
                        isGeneric,
                        numFunctionPointerTypeParameters,
                        allocatedNonGCDataSize != 0,
                        state.GcStaticDesc != IntPtr.Zero,
                        state.ThreadStaticDesc != IntPtr.Zero);

                    // Dynamic types have an extra pointer-sized field that contains a pointer to their template type
                    cbEEType += IntPtr.Size;

                    MethodTable* elementEEType = null;
                    int cbGCDesc = isMdArray
                        ? GetMdArrayGCDescSize((ArrayType)state.TypeBeingBuilt, out elementEEType)
                        : RuntimeAugments.GetGCDescSize(pTemplateEEType->ToRuntimeTypeHandle());
                    int cbGCDescAligned = MemoryHelpers.AlignUp(cbGCDesc, IntPtr.Size);

                    // Allocate enough space for the MethodTable + gcDescSize
                    eeTypePlusGCDesc = MemoryHelpers.AllocateMemory(cbGCDescAligned + cbEEType);

                    // Get the MethodTable pointer, and the template MethodTable pointer
                    pEEType = (MethodTable*)((byte*)eeTypePlusGCDesc + cbGCDescAligned);
                    state.HalfBakedRuntimeTypeHandle = pEEType->ToRuntimeTypeHandle();

                    // Set basic MethodTable fields
                    pEEType->Flags = flags;
                    pEEType->RawBaseSize = (uint)baseSize;
                    pEEType->NumVtableSlots = numVtableSlots;
                    pEEType->NumInterfaces = runtimeInterfacesLength;
                    pEEType->HashCode = hashCodeOfNewType;
                    pEEType->PointerToTypeManager = pTemplateEEType->PointerToTypeManager;

                    if (isMdArray)
                    {
                        CreateMdArrayGCDesc(elementEEType, pEEType, cbGCDesc);
                    }
                    else
                    {
                        // Specific-canonical templates have the same instance layout, including SZ-array elements.
                        Buffer.MemoryCopy((byte*)pTemplateEEType - cbGCDesc, (byte*)pEEType - cbGCDesc, cbGCDesc, cbGCDesc);
                    }
                    Debug.Assert(RuntimeAugments.GetGCDescSize(pEEType->ToRuntimeTypeHandle()) == cbGCDesc);

                    // Copy VTable entries from template type
                    IntPtr* pVtable = (IntPtr*)((byte*)pEEType + sizeof(MethodTable));
                    IntPtr* pTemplateVtable = (IntPtr*)((byte*)pTemplateEEType + sizeof(MethodTable));
                    for (int i = 0; i < numVtableSlots; i++)
                        pVtable[i] = pTemplateVtable[i];

                    // Copy dispatch map from the template type
                    if (hasDispatchMap)
                    {
                        pEEType->DispatchMap = pTemplateEEType->DispatchMap;
                    }

                    // Copy Pointer to finalizer method from the template type
                    if (hasFinalizer)
                    {
                        pEEType->FinalizerCode = pTemplateEEType->FinalizerCode;
                    }
                }

                // Copy the sealed vtable entries if they exist on the template type
                if (hasSealedVTable)
                {
                    uint cbSealedVirtualSlotsTypeOffset = pEEType->GetFieldOffset(EETypeField.ETF_SealedVirtualSlots);
                    *((void**)((byte*)pEEType + cbSealedVirtualSlotsTypeOffset)) = pTemplateEEType->GetSealedVirtualTable();
                }

                writableData = MemoryHelpers.AllocateMemory(WritableData.GetSize(IntPtr.Size));
                NativeMemory.Clear(writableData, (nuint)WritableData.GetSize(IntPtr.Size));
                pEEType->WritableData = writableData;

                pEEType->DynamicTemplateType = pTemplateEEType;
                pEEType->DynamicTypeFlags = dynamicTypeFlags;

                int nonGCStaticDataOffset = state.HasStaticConstructor ? -TypeBuilder.ClassConstructorOffset : 0;

                if (isGeneric)
                {
                    if (arity > 1)
                    {
                        genericComposition = MemoryHelpers.AllocateMemory(MethodTable.GetGenericCompositionSize(arity));
                        pEEType->SetGenericComposition((IntPtr)genericComposition);
                    }

                    if (allocatedNonGCDataSize > 0)
                    {
                        nonGcStaticData = MemoryHelpers.AllocateMemory(allocatedNonGCDataSize);
                        NativeMemory.Clear(nonGcStaticData, (nuint)allocatedNonGCDataSize);
                        Debug.Assert(nonGCStaticDataOffset <= allocatedNonGCDataSize);
                        pEEType->DynamicNonGcStaticsData = (IntPtr)((byte*)nonGcStaticData + nonGCStaticDataOffset);
                    }
                }

                if (state.ThreadStaticDesc != IntPtr.Zero)
                {
                    state.ThreadStaticOffset = TypeLoaderEnvironment.Instance.GetNextThreadStaticsOffsetValue(pEEType->TypeManager);

                    threadStaticIndex = MemoryHelpers.AllocateMemory(IntPtr.Size * 2);
                    *(IntPtr*)threadStaticIndex = pEEType->PointerToTypeManager;
                    *(((IntPtr*)threadStaticIndex) + 1) = (IntPtr)state.ThreadStaticOffset;
                    pEEType->DynamicThreadStaticsIndex = (IntPtr)threadStaticIndex;
                }

                if (state.GcStaticDesc != IntPtr.Zero)
                {
                    // Statics are allocated on GC heap
                    object obj = RuntimeAugments.RawNewObject(((MethodTable*)state.GcStaticDesc)->ToRuntimeTypeHandle());
                    gcStaticData = RuntimeAugments.RhHandleAlloc(obj, GCHandleType.Normal);

                    pEEType->DynamicGcStaticsData = (IntPtr)gcStaticData;
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
                    if (gcStaticData != 0)
                        RuntimeAugments.RhHandleFree(gcStaticData);

                    MemoryHelpers.FreeMemory((void*)state.HalfBakedDictionary);

                    MemoryHelpers.FreeMemory(threadStaticIndex);
                    MemoryHelpers.FreeMemory(nonGcStaticData);
                    MemoryHelpers.FreeMemory(genericComposition);
                    MemoryHelpers.FreeMemory(writableData);
                    MemoryHelpers.FreeMemory(eeTypePlusGCDesc);
                }
            }
        }

        private static void CreateMdArrayGCDesc(MethodTable* elementEEType, MethodTable* pEEType, int cbGCDesc)
        {
            pEEType->ContainsGCPointers = cbGCDesc != 0;
            if (cbGCDesc == 0)
                return;

            int baseSize = (int)pEEType->BaseSize;
            nint* gcDesc = (nint*)pEEType;
            nint* elementGCDesc = (nint*)elementEEType;
            // A series spanning the entire boxed payload has only the two header words subtracted.
            if (elementEEType == null || elementGCDesc[-3] == -2 * sizeof(nint))
            {
                Debug.Assert(cbGCDesc == 3 * sizeof(nint));
                gcDesc[-3] = -baseSize;
                gcDesc[-2] = baseSize - sizeof(nint);
                gcDesc[-1] = 1;
                return;
            }

            int elementBaseSize = (int)elementEEType->BaseSize;
            int series = (int)elementGCDesc[-1];
            int firstOffset = (int)elementGCDesc[-2];
            gcDesc[-1] = -series;
            gcDesc[-2] = baseSize - 2 * sizeof(nint) + firstOffset;
            elementGCDesc -= 2;

#if TARGET_64BIT
            uint* ptr = (uint*)(gcDesc - 2) - 1;
#else
            ushort* ptr = (ushort*)(gcDesc - 2) - 1;
#endif
            for (int i = 0; i < series; i++)
            {
                int offset = (int)*elementGCDesc--;
                int length = (int)*elementGCDesc-- + elementBaseSize;
                // The last skip wraps to the first GC pointer in the next unboxed element.
                int nextOffset = i + 1 < series
                    ? (int)*elementGCDesc
                    : firstOffset + elementBaseSize - 2 * sizeof(nint);
                Debug.Assert(length > 0 && nextOffset >= offset + length);
                *ptr-- = (ushort)(nextOffset - offset - length);
                *ptr-- = (ushort)(length / sizeof(nint));
            }
            Debug.Assert(cbGCDesc == (byte*)gcDesc - (byte*)(ptr + 1));
        }

        private static int GetMdArrayGCDescSize(ArrayType arrayType, out MethodTable* elementEEType)
        {
            Debug.Assert(arrayType.IsMdArray);
            elementEEType = null;
            TypeDesc elementType = arrayType.ElementType;
            if (!elementType.IsValueType)
            {
                Debug.Assert(!elementType.IsByRef);
                return elementType.IsPointer || elementType.IsFunctionPointer ? 0 : 3 * sizeof(nint);
            }

            RuntimeTypeHandle elementHandle = elementType.GetRuntimeTypeHandle();
            if (elementHandle.IsNull())
                elementHandle = elementType.ComputeTemplate().RuntimeTypeHandle;

            elementEEType = elementHandle.ToEETypePtr();
            if (!elementEEType->ContainsGCPointers)
                return 0;

            int series = (int)((nint*)elementEEType)[-1];
            Debug.Assert(series > 0);
            return (series + 2) * sizeof(nint);
        }

        public static RuntimeTypeHandle CreateFunctionPointerEEType(uint hashCodeOfNewType, RuntimeTypeHandle returnTypeHandle, RuntimeTypeHandle[] parameterHandles, FunctionPointerType functionPointerType)
        {
            TypeBuilderState state = new TypeBuilderState(functionPointerType);

            CreateEETypeWorker(typeof(delegate*<void>).TypeHandle.ToEETypePtr(), hashCodeOfNewType, 0, state);
            Debug.Assert(!state.HalfBakedRuntimeTypeHandle.IsNull());

            TypeLoaderLogger.WriteLine("Allocated new FUNCTION POINTER type " + functionPointerType.ToString() + " with hashcode value = 0x" + hashCodeOfNewType.LowLevelToString() + " with MethodTable = " + state.HalfBakedRuntimeTypeHandle.ToIntPtr().LowLevelToString());

            state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->FunctionPointerReturnType = returnTypeHandle.ToEETypePtr();
            Debug.Assert(state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->NumFunctionPointerParameters == parameterHandles.Length);
            MethodTableList paramList = state.HalfBakedRuntimeTypeHandle.ToEETypePtr()->FunctionPointerParameters;
            for (int i = 0; i < parameterHandles.Length; i++)
                paramList[i] = parameterHandles[i].ToEETypePtr();

            return state.HalfBakedRuntimeTypeHandle;
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
                Debug.Assert(!state.HasStaticConstructor);
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
            DefType baseType = type.BaseType;
            if (baseType is null)
                return 0;

            RuntimeTypeHandle baseTypeHandle = baseType.GetRuntimeTypeHandle();
            if (baseTypeHandle.IsNull())
                baseTypeHandle = baseType.ComputeTemplate().RuntimeTypeHandle;

            return baseTypeHandle.ToEETypePtr()->NumVtableSlots;
        }
    }
}
