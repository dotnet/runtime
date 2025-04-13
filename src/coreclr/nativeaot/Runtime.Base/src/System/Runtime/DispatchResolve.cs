// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Runtime;

namespace System.Runtime
{
    internal static unsafe class DispatchResolve
    {
        public static IntPtr FindInterfaceMethodImplementationTarget(MethodTable* pTgtType,
                                                                 MethodTable* pItfType,
                                                                 ushort itfSlotNumber,
                                                                 ResolveFlags flags,
                                                                 /* out */ MethodTable** ppGenericContext)
        {
            // We set this bit below during second pass, callers should not set it.
            Debug.Assert((flags & ResolveFlags.DefaultInterfaceImplementation) == 0);

            // Start at the current type and work up the inheritance chain
            MethodTable* pCur = pTgtType;

        again:
            while (pCur != null)
            {
                ushort implSlotNumber;
                if (FindImplSlotForCurrentType(
                        pCur, pItfType, itfSlotNumber, flags, &implSlotNumber, ppGenericContext))
                {
                    IntPtr targetMethod;
                    if (implSlotNumber < pCur->NumVtableSlots)
                    {
                        // true virtual - need to get the slot from the target type in case it got overridden
                        targetMethod = pTgtType->GetVTableStartAddress()[implSlotNumber];
                    }
                    else if (implSlotNumber == SpecialDispatchMapSlot.Reabstraction)
                    {
                        throw pTgtType->GetClasslibException(ExceptionIDs.EntrypointNotFound);
                    }
                    else if (implSlotNumber == SpecialDispatchMapSlot.Diamond)
                    {
                        throw pTgtType->GetClasslibException(ExceptionIDs.AmbiguousImplementation);
                    }
                    else
                    {
                        // sealed virtual - need to get the slot form the implementing type, because
                        // it's not present on the target type
                        targetMethod = pCur->GetSealedVirtualSlot((ushort)(implSlotNumber - pCur->NumVtableSlots));
                    }
                    return targetMethod;
                }
                if (pCur->IsArray)
                    pCur = pCur->GetArrayEEType();
                else
                    pCur = pCur->NonArrayBaseType;
            }

            // If we haven't found an implementation, do a second pass looking for a default implementation.
            if ((flags & ResolveFlags.DefaultInterfaceImplementation) == 0)
            {
                flags |= ResolveFlags.DefaultInterfaceImplementation;
                pCur = pTgtType;
                goto again;
            }

            return IntPtr.Zero;
        }


        private static bool FindImplSlotForCurrentType(MethodTable* pTgtType,
                                        MethodTable* pItfType,
                                        ushort itfSlotNumber,
                                        ResolveFlags flags,
                                        ushort* pImplSlotNumber,
                                        MethodTable** ppGenericContext)
        {
            // We set this below during second pass, callers should not set this.
            Debug.Assert((flags & ResolveFlags.Variant) == 0);

            bool fRes = false;

            // If making a call and doing virtual resolution don't look into the dispatch map,
            // take the slot number directly.
            if (!pItfType->IsInterface)
            {
                *pImplSlotNumber = itfSlotNumber;

                // Only notice matches if the target type and search types are the same
                // This will make dispatch to sealed slots work correctly
                return pTgtType == pItfType;
            }

            if (pTgtType->HasDispatchMap)
            {
                // For variant interface dispatch, the algorithm is to walk the parent hierarchy, and at each level
                // attempt to dispatch exactly first, and then if that fails attempt to dispatch variantly. This can
                // result in interesting behavior such as a derived type only overriding one particular instantiation
                // and funneling all the dispatches to it, but its the algorithm.

                fRes = FindImplSlotInSimpleMap(
                    pTgtType, pItfType, itfSlotNumber, pImplSlotNumber, ppGenericContext, flags);

                if (!fRes)
                {
                    flags |= ResolveFlags.Variant; // check variance for second scan of dispatch map
                    fRes = FindImplSlotInSimpleMap(
                     pTgtType, pItfType, itfSlotNumber, pImplSlotNumber, ppGenericContext, flags);
                }
            }

            return fRes;
        }

        private static bool FindImplSlotInSimpleMap(MethodTable* pTgtType,
                                     MethodTable* pItfType,
                                     uint itfSlotNumber,
                                     ushort* pImplSlotNumber,
                                     MethodTable** ppGenericContext,
                                     ResolveFlags flags)
        {
            Debug.Assert(pTgtType->HasDispatchMap, "Missing dispatch map");

            MethodTable* pItfOpenGenericType = null;
            MethodTableList itfInstantiation = default;
            int itfArity = 0;
            GenericVariance* pItfVarianceInfo = null;

            bool fCheckVariance = false;
            bool fArrayCovariance = false;

            if ((flags & ResolveFlags.Variant) != 0)
            {
                fCheckVariance = pItfType->HasGenericVariance;
                fArrayCovariance = pTgtType->IsArray;

                // Non-arrays can follow array variance rules iff
                // 1. They have one generic parameter
                // 2. That generic parameter is array covariant.
                //
                // This special case is to allow array enumerators to work
                if (!fArrayCovariance && pTgtType->HasGenericVariance)
                {
                    int tgtEntryArity = (int)pTgtType->GenericArity;
                    GenericVariance* pTgtVarianceInfo = pTgtType->GenericVariance;

                    if ((tgtEntryArity == 1) && pTgtVarianceInfo[0] == GenericVariance.ArrayCovariant)
                    {
                        fArrayCovariance = true;
                    }
                }

                // Arrays are covariant even though you can both get and set elements (type safety is maintained by
                // runtime type checks during set operations). This extends to generic interfaces implemented on those
                // arrays. We handle this by forcing all generic interfaces on arrays to behave as though they were
                // covariant (over their one type parameter corresponding to the array element type).
                if (fArrayCovariance && pItfType->IsGeneric)
                    fCheckVariance = true;

                // If there is no variance checking, there is no operation to perform. (The non-variance check loop
                // has already completed)
                if (!fCheckVariance)
                {
                    return false;
                }
            }

            bool fStaticDispatch = (flags & ResolveFlags.Static) != 0;
            bool checkDefaultImplementations = (flags & ResolveFlags.DefaultInterfaceImplementation) != 0;

            // We either scan the instance or static portion of the dispatch map. Depends on what the caller wants.
            DispatchMap* pMap = pTgtType->DispatchMap;
            DispatchMap.DispatchMapEntry* i = fStaticDispatch ?
                pMap->GetStaticEntry(checkDefaultImplementations ? (int)pMap->NumStandardStaticEntries : 0) :
                pMap->GetEntry(checkDefaultImplementations ? (int)pMap->NumStandardEntries : 0);
            DispatchMap.DispatchMapEntry* iEnd = fStaticDispatch ?
                pMap->GetStaticEntry(checkDefaultImplementations ? (int)(pMap->NumStandardStaticEntries + pMap->NumDefaultStaticEntries) : (int)pMap->NumStandardStaticEntries) :
                pMap->GetEntry(checkDefaultImplementations ? (int)(pMap->NumStandardEntries + pMap->NumDefaultEntries) : (int)pMap->NumStandardEntries);
            for (; i != iEnd; i = fStaticDispatch ? (DispatchMap.DispatchMapEntry*)(((DispatchMap.StaticDispatchMapEntry*)i) + 1) : i + 1)
            {
                if (i->_usInterfaceMethodSlot == itfSlotNumber)
                {
                    MethodTable* pCurEntryType =
                        pTgtType->InterfaceMap[i->_usInterfaceIndex];

                    if (pCurEntryType == pItfType)
                    {
                        *pImplSlotNumber = i->_usImplMethodSlot;

                        // If this is a static method, the entry point is not usable without generic context.
                        // (Instance methods acquire the generic context from their `this`.)
                        // Same for IDynamicInterfaceCastable (that has a `this` but it's not useful)
                        if (fStaticDispatch)
                            *ppGenericContext = GetGenericContextSource(pTgtType, i);
                        else if ((flags & ResolveFlags.IDynamicInterfaceCastable) != 0)
                            *ppGenericContext = pTgtType;

                        return true;
                    }
                    else if (fCheckVariance && ((fArrayCovariance && pCurEntryType->IsGeneric) || pCurEntryType->HasGenericVariance))
                    {
                        // Interface types don't match exactly but both the target interface and the current interface
                        // in the map are marked as being generic with at least one co- or contra- variant type
                        // parameter. So we might still have a compatible match.

                        // Retrieve the unified generic instance for the callsite interface if we haven't already (we
                        // lazily get this then cache the result since the lookup isn't necessarily cheap).
                        if (pItfOpenGenericType == null)
                        {
                            pItfOpenGenericType = pItfType->GenericDefinition;
                            itfArity = (int)pItfType->GenericArity;
                            itfInstantiation = pItfType->GenericArguments;
                            pItfVarianceInfo = pItfType->GenericVariance;
                        }

                        // Retrieve the unified generic instance for the interface we're looking at in the map.
                        MethodTable* pCurEntryGenericType = pCurEntryType->GenericDefinition;

                        // If the generic types aren't the same then the types aren't compatible.
                        if (pItfOpenGenericType != pCurEntryGenericType)
                            continue;

                        // Grab instantiation details for the candidate interface.
                        MethodTableList curEntryInstantiation = pCurEntryType->GenericArguments;

                        // The types represent different instantiations of the same generic type. The
                        // arity of both had better be the same.
                        Debug.Assert(itfArity == (int)pCurEntryType->GenericArity, "arity mismatch between generic instantiations");

                        if (TypeCast.TypeParametersAreCompatible(itfArity, curEntryInstantiation, itfInstantiation, pItfVarianceInfo, fArrayCovariance, null))
                        {
                            *pImplSlotNumber = i->_usImplMethodSlot;

                            // If this is a static method, the entry point is not usable without generic context.
                            // (Instance methods acquire the generic context from their `this`.)
                            // Same for IDynamicInterfaceCastable (that has a `this` but it's not useful)
                            if (fStaticDispatch)
                                *ppGenericContext = GetGenericContextSource(pTgtType, i);
                            else if ((flags & ResolveFlags.IDynamicInterfaceCastable) != 0)
                                *ppGenericContext = pTgtType;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static unsafe MethodTable* GetGenericContextSource(MethodTable* pTgtType, DispatchMap.DispatchMapEntry* pEntry)
        {
            ushort usEncodedValue = ((DispatchMap.StaticDispatchMapEntry*)pEntry)->_usContextMapSource;
            return usEncodedValue switch
            {
                StaticVirtualMethodContextSource.None => null,
                StaticVirtualMethodContextSource.ContextFromThisClass => pTgtType,
                _ => pTgtType->InterfaceMap[usEncodedValue - StaticVirtualMethodContextSource.ContextFromFirstInterface]
            };
        }

        public enum ResolveFlags
        {
            Variant = 0x1,
            DefaultInterfaceImplementation = 0x2,
            Static = 0x4,
            IDynamicInterfaceCastable = 0x8,
        }
    }
}
