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
                                                                 ushort itfSlotNumber)
        {
            DynamicModule* dynamicModule = pTgtType->DynamicModule;

            // Use the dynamic module resolver if it's present
            if (dynamicModule != null)
            {
                delegate*<MethodTable*, MethodTable*, ushort, IntPtr> resolver = dynamicModule->DynamicTypeSlotDispatchResolve;
                if (resolver != null)
                    return resolver(pTgtType, pItfType, itfSlotNumber);
            }

            // Start at the current type and work up the inheritance chain
            MethodTable* pCur = pTgtType;

            if (pItfType->IsCloned)
                pItfType = pItfType->CanonicalEEType;

            while (pCur != null)
            {
                ushort implSlotNumber;
                if (FindImplSlotForCurrentType(
                        pCur, pItfType, itfSlotNumber, &implSlotNumber))
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
            return IntPtr.Zero;
        }


        private static bool FindImplSlotForCurrentType(MethodTable* pTgtType,
                                        MethodTable* pItfType,
                                        ushort itfSlotNumber,
                                        ushort* pImplSlotNumber)
        {
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
                // We first look at non-default implementation. Default implementations are only considered
                // if the "old algorithm" didn't come up with an answer.

                bool fDoDefaultImplementationLookup = false;

                // For variant interface dispatch, the algorithm is to walk the parent hierarchy, and at each level
                // attempt to dispatch exactly first, and then if that fails attempt to dispatch variantly. This can
                // result in interesting behavior such as a derived type only overriding one particular instantiation
                // and funneling all the dispatches to it, but its the algorithm.

            again:
                bool fDoVariantLookup = false; // do not check variance for first scan of dispatch map

                fRes = FindImplSlotInSimpleMap(
                    pTgtType, pItfType, itfSlotNumber, pImplSlotNumber, fDoVariantLookup, fDoDefaultImplementationLookup);

                if (!fRes)
                {
                    fDoVariantLookup = true; // check variance for second scan of dispatch map
                    fRes = FindImplSlotInSimpleMap(
                     pTgtType, pItfType, itfSlotNumber, pImplSlotNumber, fDoVariantLookup, fDoDefaultImplementationLookup);
                }

                // If we haven't found anything and haven't looked at the default implementations yet, look now
                if (!fRes && !fDoDefaultImplementationLookup)
                {
                    fDoDefaultImplementationLookup = true;
                    goto again;
                }
            }

            return fRes;
        }

        private static bool FindImplSlotInSimpleMap(MethodTable* pTgtType,
                                     MethodTable* pItfType,
                                     uint itfSlotNumber,
                                     ushort* pImplSlotNumber,
                                     bool actuallyCheckVariance,
                                     bool checkDefaultImplementations)
        {
            Debug.Assert(pTgtType->HasDispatchMap, "Missing dispatch map");

            MethodTable* pItfOpenGenericType = null;
            EETypeRef* pItfInstantiation = null;
            int itfArity = 0;
            GenericVariance* pItfVarianceInfo = null;

            bool fCheckVariance = false;
            bool fArrayCovariance = false;

            if (actuallyCheckVariance)
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

            DispatchMap* pMap = pTgtType->DispatchMap;
            DispatchMap.DispatchMapEntry* i = (*pMap)[checkDefaultImplementations ? (int)pMap->NumStandardEntries : 0];
            DispatchMap.DispatchMapEntry* iEnd = (*pMap)[checkDefaultImplementations ? (int)(pMap->NumStandardEntries + pMap->NumDefaultEntries) : (int)pMap->NumStandardEntries];
            for (; i != iEnd; ++i)
            {
                if (i->_usInterfaceMethodSlot == itfSlotNumber)
                {
                    MethodTable* pCurEntryType =
                        pTgtType->InterfaceMap[i->_usInterfaceIndex].InterfaceType;

                    if (pCurEntryType->IsCloned)
                        pCurEntryType = pCurEntryType->CanonicalEEType;

                    if (pCurEntryType == pItfType)
                    {
                        *pImplSlotNumber = i->_usImplMethodSlot;
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
                            pItfInstantiation = pItfType->GenericArguments;
                            pItfVarianceInfo = pItfType->GenericVariance;
                        }

                        // Retrieve the unified generic instance for the interface we're looking at in the map.
                        MethodTable* pCurEntryGenericType = pCurEntryType->GenericDefinition;

                        // If the generic types aren't the same then the types aren't compatible.
                        if (pItfOpenGenericType != pCurEntryGenericType)
                            continue;

                        // Grab instantiation details for the candidate interface.
                        EETypeRef* pCurEntryInstantiation = pCurEntryType->GenericArguments;

                        // The types represent different instantiations of the same generic type. The
                        // arity of both had better be the same.
                        Debug.Assert(itfArity == (int)pCurEntryType->GenericArity, "arity mismatch betweeen generic instantiations");

                        if (TypeCast.TypeParametersAreCompatible(itfArity, pCurEntryInstantiation, pItfInstantiation, pItfVarianceInfo, fArrayCovariance, null))
                        {
                            *pImplSlotNumber = i->_usImplMethodSlot;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
