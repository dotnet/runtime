// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.Augments;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.Runtime.CallConverter;

using ArgIterator = Internal.Runtime.CallConverter.ArgIterator;

namespace Internal.Runtime.TypeLoader
{
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
    public static class LazyVTableResolver
    {
        private static object s_lockObject = new object();
        private static object s_lazyVtableThunksPoolHeap;

        private static volatile IntPtr[] s_thunks = InitialThunks();

        [DllImport("*", ExactSpelling = true, EntryPoint = "VTableResolver_Init")]
        private static extern unsafe int VTableResolver_Init(out IntPtr firstResolverThunk,
                                                     IntPtr vtableResolveCallback,
                                                     IntPtr universalTransition,
                                                     out int pregeneratedThunkCount);

        [DllImport("*", ExactSpelling = true, EntryPoint = "VTableResolver_GetCommonCallingStub")]
        private static extern unsafe IntPtr VTableResolver_GetCommonCallingStub();

        /// <summary>
        /// Build initial array of vtable thunks. These thunks are the ones directly embedded in
        /// the typeloader codebase instead of being dynamically generated out of the thunk pool.
        /// </summary>
        private static IntPtr[] InitialThunks()
        {
            IntPtr firstResolverThunk;
            int thunkCount;
            int thunkSize = VTableResolver_Init(out firstResolverThunk,
                                                Intrinsics.AddrOf(new Func<IntPtr, IntPtr, IntPtr>(VTableResolveThunk)),
                                                RuntimeAugments.GetUniversalTransitionThunk(),
                                                out thunkCount);
            IntPtr[] initialThunks = new IntPtr[thunkCount];
            for (int i = 0; i < thunkCount; i++)
            {
                unsafe
                {
                    initialThunks[i] = (IntPtr)(((byte*)firstResolverThunk) + (thunkSize * i));
                }
            }

            return initialThunks;
        }

        /// <summary>
        /// Get a thunk for resolving a call on a particular vtable slot index.
        /// </summary>
        public static IntPtr GetThunkForSlot(int slotIndex)
        {
            IntPtr[] currentThunks = s_thunks;
            if ((currentThunks.Length > slotIndex) && (currentThunks[slotIndex] != IntPtr.Zero))
                return currentThunks[slotIndex];
            else
            {
                lock (s_lockObject)
                {
                    currentThunks = s_thunks;

                    if ((currentThunks.Length > slotIndex) && (currentThunks[slotIndex] != IntPtr.Zero))
                        return currentThunks[slotIndex];

                    if ((currentThunks.Length <= slotIndex))
                    {
                        // Need to grow thunk collection
                        int newLength = currentThunks.Length;
                        while (newLength <= slotIndex)
                            newLength *= 2;

                        Array.Resize(ref currentThunks, newLength);
                        s_thunks = currentThunks;
                    }

                    // Now that we are certain that the array has enough space to store the thunk pointer,
                    // create a thunk.
                    return currentThunks[slotIndex] = CreateDynamicThunk(slotIndex);
                }
            }
        }

        private static IntPtr CreateDynamicThunk(int slotIndex)
        {
            if (s_lazyVtableThunksPoolHeap == null)
            {
                s_lazyVtableThunksPoolHeap = RuntimeAugments.CreateThunksHeap(VTableResolver_GetCommonCallingStub());
                Debug.Assert(s_lazyVtableThunksPoolHeap != null);
            }

            IntPtr thunk = RuntimeAugments.AllocateThunk(s_lazyVtableThunksPoolHeap);
            if (thunk == IntPtr.Zero)
                Environment.FailFast("Not enough thunks for calling convention converter");

            int eetypeVtableOffset = SlotIndexToEETypeVTableOffset(slotIndex);

            RuntimeAugments.SetThunkData(s_lazyVtableThunksPoolHeap, thunk, new IntPtr(eetypeVtableOffset), thunk);

            return thunk;
        }

        /// <summary>
        /// Convert a virtual slot index to a vtable offset from the start of an MethodTable
        /// </summary>
        public static unsafe int SlotIndexToEETypeVTableOffset(int slotIndex)
        {
            if (slotIndex < 0)
                throw new BadImageFormatException();

            return sizeof(MethodTable) + (slotIndex * IntPtr.Size);
        }

        /// <summary>
        /// Get the thunk that can represent all finalizers of metadata represented objects.
        /// </summary>
        public static IntPtr GetFinalizerThunk()
        {
            // Here we take advantage of the detail that the calling convention abi for a static function
            // that returns no values, and takes a single object parameter matches that of a
            // instance function that takes no parameters
            return Intrinsics.AddrOf(new Action<object>(FinalizeThunk));
        }

        private static unsafe int EETypeVTableOffsetToSlotIndex(int eeTypeVTableOffset)
        {
            return (eeTypeVTableOffset - sizeof(MethodTable)) / IntPtr.Size;
        }

        /// <summary>
        /// This function is called from the lazy vtable resolver thunks via the UniversalTransitionThunk to compute
        /// the correct resolution of a virtual dispatch.
        /// </summary>
        /// <param name="callerTransitionBlockParam">pointer to the arguments of the called function</param>
        /// <param name="eeTypePointerOffsetAsIntPtr">eeTypePointerOffsetAsIntPtr is the offset from the start of the MethodTable to the vtable slot</param>
        /// <returns>function pointer of correct override of virtual function</returns>
        private static unsafe IntPtr VTableResolveThunk(IntPtr callerTransitionBlockParam, IntPtr eeTypePointerOffsetAsIntPtr)
        {
            int eeTypePointerOffset = (int)eeTypePointerOffsetAsIntPtr;
            int vtableSlotIndex = EETypeVTableOffsetToSlotIndex(eeTypePointerOffset);
            Debug.Assert(eeTypePointerOffset == SlotIndexToEETypeVTableOffset(vtableSlotIndex)); // Assert that the round trip through the slot calculations is good

            MethodTable** thisPointer = *((MethodTable***)(((byte*)callerTransitionBlockParam) + ArgIterator.GetThisOffset()));
            MethodTable* MethodTable = *thisPointer;

            RuntimeTypeHandle rth = MethodTable->ToRuntimeTypeHandle();

            TypeSystemContext context = TypeSystemContextFactory.Create();
            TypeDesc type = context.ResolveRuntimeTypeHandle(rth);

            IntPtr functionPointer = ResolveVirtualVTableFunction(type, vtableSlotIndex);
            MethodTable->GetVTableStartAddress()[vtableSlotIndex] = functionPointer;

            TypeSystemContextFactory.Recycle(context);

            return functionPointer;
        }

        /// <summary>
        /// This thunk is used to lazily resolve Finalizer calls
        /// </summary>
        /// <param name="obj">Object to be finalized</param>
        private static void FinalizeThunk(object obj)
        {
            RuntimeTypeHandle rthType = RuntimeAugments.GetRuntimeTypeHandleFromObjectReference(obj);
            TypeSystemContext context = TypeSystemContextFactory.Create();
            TypeDesc type = context.ResolveRuntimeTypeHandle(rthType);

            MethodDesc finalizer = type.GetFinalizer();
            IntPtr fnPtrFinalizer;
            if (!TryGetVTableCallableAddress(finalizer, out fnPtrFinalizer))
            {
                Environment.FailFast("Method address lookup failed for: " + finalizer.ToString());
            }

            TypeSystemContextFactory.Recycle(context);

            unsafe
            {
                rthType.ToEETypePtr()->FinalizerCode = fnPtrFinalizer;
            }

            // Call the finalizer directly. No need to play tricks with tail calling, as this is rare enough, it shouldn't happen much

            // Here we take advantage of the detail that the calling convention abi for a static function
            // that returns no values, and takes a single object parameter matches that of a
            // instance function that takes no parameters
            Intrinsics.Call(fnPtrFinalizer, obj);
        }

        private static IntPtr ResolveVirtualVTableFunction(TypeDesc type, int vtableSlotIndex)
        {
            IntPtr exactResult;
            MethodDesc virtualFunctionDefiningSlot = ResolveVTableSlotIndexToMethodDescOrFunctionPointer(type.GetClosestDefType(), vtableSlotIndex, out exactResult);
            if (virtualFunctionDefiningSlot == null)
                return exactResult;

            MethodDesc virtualFunctionOverride = ((MetadataType)type.GetClosestDefType()).FindVirtualFunctionTargetMethodOnObjectType(virtualFunctionDefiningSlot);

            if (TryGetVTableCallableAddress(virtualFunctionOverride, out exactResult))
                return exactResult;

            Environment.FailFast("Method address lookup failed for: " + virtualFunctionOverride.ToString());
            return IntPtr.Zero;
        }

        /// <summary>
        /// Get the virtual slot index of a given virtual method. (This is only valid for non-interface virtuals)
        /// </summary>
        /// <param name="virtualMethod">virtual method to get slot index of</param>
        /// <returns>slot index, or -1</returns>
        public static int VirtualMethodToSlotIndex(MethodDesc virtualMethod)
        {
            Debug.Assert(virtualMethod.IsVirtual);
            Debug.Assert(!virtualMethod.OwningType.IsInterface);

            MethodDesc definingMethod = virtualMethod;

            // If not newslot, make sure we've got the defining method here
            if (!definingMethod.IsNewSlot)
            {
                definingMethod = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(definingMethod);
            }
            TypeDesc definingType = definingMethod.OwningType;

            // Two possible cases for determining slot index that will work
            //  1. The definingType is a R2R type with full metadata. Compute the MethodDesc, by scanning the list of virtuals present in metadata. Its possible to not get a slot index. In that case return -1
            //  2. The definingType is pregenerated, but we can go from metadata to slot index via the runtime mapping tables.

            if (!IsPregeneratedOrTemplateTypeLoaded(definingType))
            {
                // Case 1

                MetadataType definingMetadataType = (MetadataType)definingType;
                int baseTypeSlotCount = 0;

                if (definingMetadataType.BaseType != null)
                {
                    unsafe
                    {
                        if (definingMetadataType.BaseType.RetrieveRuntimeTypeHandleIfPossible())
                        {
                            baseTypeSlotCount = definingMetadataType.BaseType.GetRuntimeTypeHandle().ToEETypePtr()->NumVtableSlots;
                        }
                        else
                        {
                            baseTypeSlotCount = definingMetadataType.BaseType.GetOrCreateTypeBuilderState().NumVTableSlots;
                        }
                    }
                }

                int currentSlot = baseTypeSlotCount;

                if (definingMetadataType.ConvertToCanonForm(CanonicalFormKind.Specific).IsCanonicalSubtype(CanonicalFormKind.Specific))
                {
                    // Deal with the space reserved for the canonical dictionary
                    currentSlot++;
                }

                foreach (MethodDesc method in definingMetadataType.GetMethods())
                {
                    if (!MethodDefinesVTableSlot(method))
                        continue;

                    if (method == definingMethod)
                        return currentSlot;
                    else
                        currentSlot++;
                }

                // No slot index defined.
                return -1;
            }
            else
            {
                // Case 2, pregenerated type
                TypeSystem.NativeFormat.NativeFormatMethod definingMethodOpenType = (TypeSystem.NativeFormat.NativeFormatMethod)definingMethod.GetTypicalMethodDefinition();
                MethodSignatureComparer methodSignatureComparer = new MethodSignatureComparer(
                    definingMethodOpenType.MetadataReader, definingMethodOpenType.Handle);

                if (!definingType.RetrieveRuntimeTypeHandleIfPossible())
                {
                    new TypeBuilder().BuildType(definingType);
                }

                TypeSystem.NativeFormat.NativeFormatType definingNativeFormatType = (TypeSystem.NativeFormat.NativeFormatType)definingType.GetTypeDefinition();
                NativeFormatModuleInfo moduleToLookIn = definingNativeFormatType.MetadataUnit.RuntimeModuleInfo;

                TypeLoaderEnvironment.VirtualResolveDataResult virtualSlotInfo;
                if (!TypeLoaderEnvironment.TryGetVirtualResolveData(moduleToLookIn, definingType.RuntimeTypeHandle, Array.Empty<RuntimeTypeHandle>(), ref methodSignatureComparer, out virtualSlotInfo))
                    return -1;

                Debug.Assert(!virtualSlotInfo.IsGVM);
                return virtualSlotInfo.SlotIndex;
            }
        }

        /// <summary>
        /// Given a type, and vtable slot index, compute either the NativeFormatMethod that defines that vtable slot,
        /// OR the implementation function pointer if the method doesn't have sufficient metadata to be interesting
        /// to use the virtual function resolution algorithm on.
        /// </summary>
        /// <param name="type">Type on which virtual resolution is to be completed</param>
        /// <param name="vtableSlotIndex">Virtual slot index which is to be examined</param>
        /// <param name="functionPointer">If there is no corresponding method defined in metadata, this is
        /// the function pointer that should be used for calls to this vtable slot</param>
        /// <returns>MethodDesc of function that defined the slot if possible.</returns>
        private static unsafe MethodDesc ResolveVTableSlotIndexToMethodDescOrFunctionPointer(DefType type, int vtableSlotIndex, out IntPtr functionPointer)
        {
            Debug.Assert(type.RetrieveRuntimeTypeHandleIfPossible());
            Debug.Assert(type.RuntimeTypeHandle.ToEETypePtr()->NumVtableSlots > vtableSlotIndex);
            DefType definingTypeScan = type;
            DefType previousDefiningType = null;
            MethodTable* typePtr = null;
            DefType definingType = null;
            functionPointer = IntPtr.Zero;

            while (true)
            {
                definingTypeScan.RetrieveRuntimeTypeHandleIfPossible();
                Debug.Assert(!definingTypeScan.RuntimeTypeHandle.IsNull());
                typePtr = definingTypeScan.RuntimeTypeHandle.ToEETypePtr();
                if (typePtr->NumVtableSlots > vtableSlotIndex)
                {
                    previousDefiningType = definingTypeScan;
                    definingTypeScan = definingTypeScan.BaseType;

                    // We found a slot on System.Object
                    if (definingTypeScan == null)
                    {
                        definingType = previousDefiningType;
                        break;
                    }
                }
                else
                {
                    // We've gone past the type in the type hierarchy that declared this vtable slot
                    // the defining type is the one we looked at previously
                    definingType = previousDefiningType;
                    break;
                }
            }

            // At this point, we know the type that definined the virtual slot
            // There are 4 possibilities here
            //  1. The definingType is a R2R type with full metadata. Compute the MethodDesc, by scanning the list of virtuals present in metadata
            //  2. The definingType is pregenerated, but we can go from the slot index to metadata via the runtime mapping tables. Do so, then run
            //     normal algorithm
            //  3. The definingType is pregenerated, but we cannot go from the slot index to metadata via the runtime mapping tables. There is
            //      only 1 pointer in the vtable of the most derived pregenerated type that has the same value. That's the valuable pointer.
            //  4. The definingType is pregenerated, but we cannot go from the slot index to metadata via the runtime mapping tables. There are
            //      multiple pointers in the vtable of the most derived pregenerated types which have this same value.
            //         - Take that pointer value, and attempt to resolve back to a method from the implementation. If that succeeds, then
            //           treat that as the correct vtable slot. Otherwise, return that function pointer. (This is a very rare scenario.)
            MethodDesc slotDefiningMethod = null;
            if (!IsPregeneratedOrTemplateTypeLoaded(definingType))
            {
                // Case 1

                MetadataType definingMetadataType = (MetadataType)definingType;
                int baseTypeSlotCount = 0;

                if (definingMetadataType.BaseType != null)
                    baseTypeSlotCount = definingMetadataType.BaseType.GetRuntimeTypeHandle().ToEETypePtr()->NumVtableSlots;

                int slotOnType = vtableSlotIndex - baseTypeSlotCount;
                Debug.Assert(slotOnType >= 0);

                // R2R types create new slots only for methods that are marked as NewSlot
                if (definingMetadataType.ConvertToCanonForm(CanonicalFormKind.Specific) != definingType)
                {
                    // Deal with the space reserved for the canonical dictionary
                    slotOnType--;
                }
                Debug.Assert(slotOnType >= 0);

                int currentSlot = 0;
                foreach (MethodDesc method in definingMetadataType.GetMethods())
                {
                    if (!MethodDefinesVTableSlot(method))
                        continue;

                    if (currentSlot == slotOnType)
                    {
                        Debug.Assert(VirtualMethodToSlotIndex(method) == vtableSlotIndex);
                        return method;
                    }
                    else
                        currentSlot++;
                }

                Environment.FailFast("Unexpected failure to find virtual function that defined slot");
                return null;
            }
            else if (TryGetVirtualMethodFromSlot(definingType, vtableSlotIndex, out slotDefiningMethod))
            {
                // Case 2
                Debug.Assert(VirtualMethodToSlotIndex(slotDefiningMethod) == vtableSlotIndex);
                return slotDefiningMethod;
            }
            else
            {
                TypeDesc mostDerivedPregeneratedType = GetMostDerivedPregeneratedOrTemplateLoadedType(type);

                MethodTable* mostDerivedTypeEEType = mostDerivedPregeneratedType.GetRuntimeTypeHandle().ToEETypePtr();
                IntPtr* vtableStart = (IntPtr*)(((byte*)mostDerivedTypeEEType) + sizeof(MethodTable));

                IntPtr possibleFunctionPointerReturn = vtableStart[vtableSlotIndex];
                int functionPointerMatches = 0;
                for (int i = 0; i < mostDerivedTypeEEType->NumVtableSlots; i++)
                {
                    if (vtableStart[i] == possibleFunctionPointerReturn)
                        functionPointerMatches++;
                }

                if (functionPointerMatches == 1)
                {
                    // Case 3
                    functionPointer = possibleFunctionPointerReturn;
                    return null;
                }
                else
                {
                    // Case 4
                    // While this case is theoretically possible, it requires MethodImpl to MethodImpl overloading for virtual functions
                    // in the non-ready to run portions of the binary. Given our current shipping plans, as this does not occur in non-obfuscated
                    // code, we will throw NotImplementedException here.
                    // The real implementation would look something like
                    // if (!TryGetNativeFormatMethodFromFunctionPointer(possibleFunctionPointerReturn, out method))
                    // {
                    //     // this method could not have been overriden in dynamically loaded code
                    //     functionPointer = possibleFunctionPointerReturn;
                    //     return null;
                    // }
                    // else
                    // {
                    //     return VirtualFunctionAlgorithm.GetDefiningMethod(method)
                    // }
                    //
                    throw NotImplemented.ByDesign;
                }
            }
        }

        private static bool TryGetVirtualMethodFromSlot(TypeDesc definingType, int vtableSlotIndex, out MethodDesc slotDefiningMethod)
        {
            MethodNameAndSignature methodNameAndSig;
            bool success = TypeLoaderEnvironment.TryGetMethodMethodNameAndSigFromVTableSlotForPregeneratedOrTemplateType
                (definingType.Context, definingType.GetRuntimeTypeHandle(), vtableSlotIndex, out methodNameAndSig);

            if (!success)
            {
                slotDefiningMethod = null;
                return false;
            }

            TypeSystem.NativeFormat.NativeFormatType metadataDefiningType = definingType.GetClosestDefType().GetTypeDefinition() as TypeSystem.NativeFormat.NativeFormatType;

            // We're working with a NoMetadataType, or an ArrayType, neither of which have full metadata
            if (metadataDefiningType == null)
            {
                slotDefiningMethod = null;
                return false;
            }

            // TryGetMethodMethodNameAndSigFromVTableSlotForPregeneratedOrTemplateType is expected to only return methodNameAndSig with NativeLayoutSignatures in them.
            // If we start hitting the more general case, we can improve this algorithm.
            Debug.Assert(methodNameAndSig.Signature.IsNativeLayoutSignature);

            foreach (TypeSystem.NativeFormat.NativeFormatMethod method in metadataDefiningType.GetMethods())
            {
                if (!method.IsVirtual)
                    continue;

                if (method.HasInstantiation)
                    continue;

                if (!method.Name.Equals(methodNameAndSig.Name))
                    continue;

                MethodSignatureComparer sigComparer = new MethodSignatureComparer(method.MetadataReader, method.Handle);
                if (!sigComparer.IsMatchingNativeLayoutMethodNameAndSignature(methodNameAndSig.Name, methodNameAndSig.Signature))
                    continue;

                // At this point we've matched
                slotDefiningMethod = method;
                return true;
            }

            // Didn't find the method
            slotDefiningMethod = null;
            return false;
        }

        /// <summary>
        /// Find the type in a type's hierarchy that wasn't generated based on metadata.
        /// </summary>
        /// <returns>found type, or null</returns>
        public static TypeDesc GetMostDerivedPregeneratedOrTemplateLoadedType(TypeDesc derivedType)
        {
            while ((derivedType != null) && !IsPregeneratedOrTemplateTypeLoaded(derivedType))
            {
                derivedType = derivedType.BaseType;
            }

            return derivedType;
        }

        public static bool IsPregeneratedOrTemplateTypeLoaded(TypeDesc derivedType)
        {
            Debug.Assert(!derivedType.IsInterface);

            DefType defTypeDerived = derivedType as DefType;

            if (defTypeDerived == null)
            {
                return true;
            }
            else
            {
                if (!(defTypeDerived is TypeSystem.NoMetadata.NoMetadataType) && !defTypeDerived.HasNativeLayout)
                {
                    if (!defTypeDerived.RetrieveRuntimeTypeHandleIfPossible())
                        return false;

                    unsafe
                    {
                        return !defTypeDerived.RuntimeTypeHandle.ToEETypePtr()->IsDynamicType;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Does a method actually recieve a VTable slot. (Must not be called for interface methods)
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool MethodDefinesVTableSlot(MethodDesc method)
        {
            Debug.Assert(!method.OwningType.IsInterface);

            if (!method.IsVirtual)
                return false;
            if (!method.IsNewSlot)
                return false;
            // Sealed virtual methods go after normal slots at the end of VTable
            if (method.IsVirtual && method.IsFinal)
                return false;
            // Generic virtuals are not in the vtable
            if (method.HasInstantiation)
                return false;

            return true;
        }

        /// <summary>
        /// Get the runtime interface slot number given an interface method
        /// </summary>
        public static bool TryGetInterfaceSlotNumberFromMethod(MethodDesc method, out ushort slot)
        {
            slot = 0;

            if (!method.OwningType.IsInterface)
                return false;

            int iSlot = 0;
            foreach (MethodDesc m in method.OwningType.GetMethods())
            {
                if (m == method)
                {
                    if (method.OwningType.IsGeneric())
                    {
                        slot = checked((ushort)(iSlot + 1));
                    }
                    else
                    {
                        slot = checked((ushort)(iSlot));
                    }
                    return true;
                }
                iSlot++;
            }

            return false;
        }

        /// <summary>
        /// Get the MethodDesc that corresponds to an interface type/slot pair
        /// </summary>
        public static bool TryGetMethodFromInterfaceSlot(TypeDesc owningType, ushort slot, out MethodDesc method)
        {
            int iMethod = -1;
            method = null;

            // This is only valid to call on an interface
            if (!owningType.IsInterface)
                return false;

            // Slots on generic interface types are off by one.
            if (owningType.IsGeneric())
            {
                if (slot == 0)
                    throw new BadImageFormatException();

                slot--;
            }

            foreach (MethodDesc searchMethod in owningType.GetMethods())
            {
                if (searchMethod.IsVirtual)
                {
                    iMethod++;
                    if (iMethod == slot)
                    {
                        method = searchMethod;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Resolve a call on the interface method targetVirtualMethod, on the type instanceDefTypeToExamine utilizing metadata to
        /// its associated virtual method.
        /// </summary>
        /// <param name="instanceDefTypeToExamine">(in) The class type on which the interface call is made, (out) the class type where the search may continue using non-metadata means</param>
        /// <param name="targetVirtualMethod">The interface method to translate into a virtual method for execution</param>
        /// <returns>virtual method slot which implements the interface method OR null if an implementation should fall back to non-metadata based lookup.</returns>
        public static MethodDesc ResolveInterfaceMethodToVirtualMethod(TypeDesc instanceType, out TypeDesc instanceDefTypeToExamine, MethodDesc targetVirtualMethod)
        {
            instanceDefTypeToExamine = instanceType.GetClosestDefType();

            MethodDesc newlyFoundVirtualMethod = null;
            LowLevelList<MethodDesc> variantTargets = null;

            if (targetVirtualMethod.OwningType.HasVariance)
            {
                foreach (TypeDesc type in instanceType.RuntimeInterfaces)
                {
                    if (type != targetVirtualMethod.OwningType &&
                        type.GetTypeDefinition() == targetVirtualMethod.OwningType.GetTypeDefinition())
                    {
                        // Check to see if these interfaces are appropriately assignable
                        if (RuntimeAugments.IsAssignableFrom(targetVirtualMethod.OwningType.GetRuntimeTypeHandle(), type.GetRuntimeTypeHandle()))
                        {
                            if (variantTargets == null)
                                variantTargets = new LowLevelList<MethodDesc>();

                            MethodDesc targetVariantMatch = type.Context.GetMethodForInstantiatedType(
                                targetVirtualMethod.GetTypicalMethodDefinition(),
                                (InstantiatedType)type);

                            variantTargets.Add(targetVariantMatch);
                        }
                    }
                }
            }

            do
            {
                newlyFoundVirtualMethod = instanceDefTypeToExamine.ResolveInterfaceMethodToVirtualMethodOnType(targetVirtualMethod);

                if (newlyFoundVirtualMethod == null && variantTargets != null)
                {
                    for (int i = 0; i < variantTargets.Count; i++)
                    {
                        newlyFoundVirtualMethod = instanceDefTypeToExamine.ResolveInterfaceMethodToVirtualMethodOnType(variantTargets[i]);
                        if (newlyFoundVirtualMethod != null)
                            break;
                    }
                }
                instanceDefTypeToExamine = instanceDefTypeToExamine.BaseType;
            } while ((newlyFoundVirtualMethod == null) && (instanceDefTypeToExamine != null) && !IsPregeneratedOrTemplateTypeLoaded(instanceDefTypeToExamine));

            return newlyFoundVirtualMethod;
        }

        /// <summary>
        /// Try to resolve a virtual call to targetMethod to its implementation on instanceType.
        /// </summary>
        /// <param name="instanceType">non-interface type</param>
        /// <param name="targetMethod">non-generic virtual or interface method</param>
        /// <param name="methodAddress">function pointer resolved</param>
        /// <returns>true if successful</returns>
        public static bool TryDispatchMethodOnTarget(TypeDesc instanceType, MethodDesc targetMethod, out IntPtr methodAddress)
        {
            methodAddress = IntPtr.Zero;

            if (targetMethod == null)
                return false;

            if (IsPregeneratedOrTemplateTypeLoaded(instanceType))
            {
                if (targetMethod.OwningType.IsInterface)
                {
                    ushort interfaceSlot;
                    if (!TryGetInterfaceSlotNumberFromMethod(targetMethod, out interfaceSlot))
                    {
                        return false;
                    }
                    methodAddress = RuntimeAugments.ResolveDispatchOnType(instanceType.GetRuntimeTypeHandle(),
                                                                          targetMethod.OwningType.GetRuntimeTypeHandle(),
                                                                          interfaceSlot);
                    Debug.Assert(methodAddress != IntPtr.Zero); // TODO! This should happen for IDynamicInterfaceCastable dispatch...
                    return true;
                }
                else
                {
                    unsafe
                    {
                        int vtableSlotIndex = LazyVTableResolver.VirtualMethodToSlotIndex(targetMethod);
                        MethodTable* MethodTable = instanceType.GetRuntimeTypeHandle().ToEETypePtr();
                        IntPtr* vtableStart = (IntPtr*)(((byte*)MethodTable) + sizeof(MethodTable));

                        methodAddress = vtableStart[vtableSlotIndex];
                        return true;
                    }
                }
            }

            MethodDesc targetVirtualMethod = targetMethod;
            DefType instanceDefType = instanceType.GetClosestDefType();

            // For interface resolution, its a two step process, first get the virtual slot
            if (targetVirtualMethod.OwningType.IsInterface)
            {
                TypeDesc instanceDefTypeToExamine;
                MethodDesc newlyFoundVirtualMethod = ResolveInterfaceMethodToVirtualMethod(instanceType, out instanceDefTypeToExamine, targetVirtualMethod);

                targetVirtualMethod = newlyFoundVirtualMethod;

                // The pregenerated type must be the one that implements the interface method
                // Call into Redhawk to deal with this.
                if ((newlyFoundVirtualMethod == null) && (instanceDefTypeToExamine != null))
                {
                    ushort interfaceSlot;
                    if (!TryGetInterfaceSlotNumberFromMethod(targetMethod, out interfaceSlot))
                    {
                        return false;
                    }
                    methodAddress = RuntimeAugments.ResolveDispatchOnType(instanceDefTypeToExamine.GetRuntimeTypeHandle(),
                                                                          targetMethod.OwningType.GetRuntimeTypeHandle(),
                                                                          interfaceSlot);

                    Debug.Assert(methodAddress != IntPtr.Zero); // TODO! This should happen for IDynamicInterfaceCastable dispatch...
                    return true;
                }
            }

            // VirtualSlot can be null if the interface method isn't really implemented. This should never happen, but since our
            // type loader doesn't check all interface overloads at load time, it could happen
            if (targetVirtualMethod == null)
                return false;

            // Resolve virtual method to exact method
            MethodDesc dispatchMethod = instanceDefType.FindVirtualFunctionTargetMethodOnObjectType(targetVirtualMethod);

            return TryGetVTableCallableAddress(dispatchMethod, out methodAddress);
        }

        /// <summary>
        /// Get an address that can go into a vtable from a method desc
        /// Based on the structure of our code, these functions are always Instance, non-generic methods
        /// and therefore, we don't need to be concerned about an extra generic dictionary parameter
        /// </summary>
        private static bool TryGetVTableCallableAddress(MethodDesc method, out IntPtr result)
        {
            TypeLoaderEnvironment.MethodAddressType dummy;
            IntPtr methodAddressNonUnboxing;
            IntPtr unboxingMethodAddress;

            if (TypeLoaderEnvironment.TryGetMethodAddressFromMethodDesc(method, out methodAddressNonUnboxing, out unboxingMethodAddress, out dummy))
            {
                if (unboxingMethodAddress != IntPtr.Zero)
                {
                    result = unboxingMethodAddress;
                }
                else
                {
                    result = methodAddressNonUnboxing;
                }
                return true;
            }

            result = IntPtr.Zero;
            return false;
        }
    }
#endif
}
