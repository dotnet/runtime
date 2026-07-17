// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.TypeLoader;

namespace Internal.Runtime
{
    internal static class Dispatch
    {
        [RuntimeExport("ResolveDispatch")]
        internal static unsafe IntPtr ResolveDispatch(object pObject, DispatchCell* pCell)
        {
            // Assume this is a static dispatch cell first
            foreach (TypeManagerHandle typeManager in StartupCodeHelpers.GetLoadedModules())
            {
                var pDispatchCellRegion = (DispatchCell*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.InterfaceDispatchCellRegion, out int length);
                if ((byte*)pCell >= (byte*)pDispatchCellRegion && (byte*)pCell < (byte*)pDispatchCellRegion + length)
                {
                    return ResolveStaticInterfaceDispatch(typeManager, pObject, (nuint)(pCell - pDispatchCellRegion), (uint)(length / sizeof(DispatchCell)));
                }

                pDispatchCellRegion = (DispatchCell*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.GvmDispatchCellRegion, out length);
                if ((byte*)pCell >= (byte*)pDispatchCellRegion && (byte*)pCell < (byte*)pDispatchCellRegion + length)
                {
                    return ResolveGvmDispatch(typeManager, pObject, (nuint)(pCell - pDispatchCellRegion), (uint)(length / sizeof(DispatchCell)));
                }
            }

            // Not found statically: must be a dynamic dispatch cell
            var pDynamicCell = (DynamicDispatchCell*)pCell;
            if (pDynamicCell->IsGvmDispatchCell)
            {
                DynamicDispatchCell.DynamicGvmDispatchCell* pDynamicGvmCell = pDynamicCell->AsGvmDispatchCell();
                return RuntimeAugments.TypeLoaderCallbacks.ResolveGenericVirtualMethodTarget(
                    new RuntimeTypeHandle(pObject.GetMethodTable()),
                    RuntimeTypeHandle.FromIntPtr(pDynamicGvmCell->OwningType),
                    pDynamicGvmCell->Handle,
                    pDynamicGvmCell->IsAsyncVariant,
                    pDynamicGvmCell->Instantiation,
                    isMethodInstantiationDataRelative: false);
            }

            DynamicDispatchCell.DynamicInterfaceDispatchCell* pDynamicInterfaceCell = pDynamicCell->AsInterfaceDispatchCell();
            return CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, (MethodTable*)pDynamicInterfaceCell->InterfaceType, (ushort)pDynamicInterfaceCell->Slot);
        }

        private static unsafe IntPtr ResolveStaticInterfaceDispatch(TypeManagerHandle typeManager, object pObject, nuint cellIndex, uint cellCount)
        {
            if (cellIndex >= cellCount)
                throw new BadImageFormatException();

            NativeParser parser = GetDispatchCellInfo(
                typeManager,
                ReadyToRunSectionType.InterfaceDispatchCellInfoRegion,
                checked((uint)cellIndex),
                cellCount,
                out ExternalReferencesTable externalReferences);

            uint interfaceTypeIndex = parser.GetUnsigned();
            uint slot = parser.GetUnsigned();

            if (slot > ushort.MaxValue)
                throw new BadImageFormatException();

            MethodTable* interfaceType = (MethodTable*)externalReferences.GetIntPtrFromIndex(interfaceTypeIndex);
            return CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, interfaceType, (ushort)slot);
        }

        private static unsafe IntPtr ResolveGvmDispatch(TypeManagerHandle typeManager, object pObject, nuint cellIndex, uint cellCount)
        {
            if (cellIndex >= cellCount)
                throw new BadImageFormatException();

            NativeParser parser = GetDispatchCellInfo(
                typeManager,
                ReadyToRunSectionType.GvmDispatchCellInfoRegion,
                checked((uint)cellIndex),
                cellCount,
                out ExternalReferencesTable externalReferences);

            uint owningTypeIndex = parser.GetUnsigned();
            uint instantiationIndex = parser.GetUnsigned();
            uint token = parser.GetUnsigned();
            uint isAsyncVariant = parser.GetUnsigned();

            if (token > int.MaxValue || isAsyncVariant > 1)
                throw new BadImageFormatException();

            MethodTable* owningType = (MethodTable*)externalReferences.GetIntPtrFromIndex(owningTypeIndex);
            void* instantiation = (void*)externalReferences.GetIntPtrFromIndex(instantiationIndex);

            return RuntimeAugments.TypeLoaderCallbacks.ResolveGenericVirtualMethodTarget(
                new RuntimeTypeHandle(pObject.GetMethodTable()),
                new RuntimeTypeHandle(owningType),
                new MethodHandle((int)token),
                isAsyncVariant != 0,
                instantiation,
                isMethodInstantiationDataRelative: MethodTable.SupportsRelativePointers);
        }

        private static unsafe NativeParser GetDispatchCellInfo(
            TypeManagerHandle typeManager,
            ReadyToRunSectionType section,
            uint cellIndex,
            uint cellCount,
            out ExternalReferencesTable externalReferences)
        {
            byte* pInfo = (byte*)RuntimeImports.RhGetModuleSection(typeManager, section, out int length);
            if (length <= 0)
                throw new BadImageFormatException();

            externalReferences = default;
            if (!externalReferences.InitializeNativeReferences(typeManager))
                throw new BadImageFormatException();

            NativeReader reader = new NativeReader(pInfo, checked((uint)length));
            NativeArray entries = new NativeArray(new NativeParser(reader, 0));
            uint entryCount = entries.GetCount();

            uint low = 0;
            uint high = entryCount;
            while (low < high)
            {
                uint middle = low + ((high - low) >> 1);
                bool found = entries.TryGetAt(middle, out NativeParser parser);
                Debug.Assert(found);

                if (parser.GetUnsigned() <= cellIndex)
                    low = middle + 1;
                else
                    high = middle;
            }

            Debug.Assert(low < entryCount);
            bool resultFound = entries.TryGetAt(low, out NativeParser result);
            Debug.Assert(resultFound);

            uint endCellIndex = result.GetUnsigned();
            Debug.Assert(endCellIndex > cellIndex && endCellIndex <= cellCount);

            return result;
        }
    }
}
