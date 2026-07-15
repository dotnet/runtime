// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerHelpers;

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

            byte* pInfo = (byte*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.InterfaceDispatchCellInfoRegion, out int length);
            int pointerSize = MethodTable.SupportsRelativePointers ? sizeof(int) : sizeof(nint);
            int descriptorSize = pointerSize + sizeof(ushort);
            byte* pointerTable;
            byte* slotTable;
            uint descriptorIndex;

            if ((ulong)length == (ulong)cellCount * (uint)descriptorSize)
            {
                pointerTable = pInfo;
                slotTable = pInfo + (nuint)cellCount * (uint)pointerSize;
                descriptorIndex = checked((uint)cellIndex);
            }
            else
            {
                GetDictionaryTables(pInfo, length, cellCount, pointerSize, descriptorSize, cellIndex,
                    out descriptorIndex, out uint descriptorCount, out pointerTable);
                slotTable = pointerTable + (nuint)descriptorCount * (uint)pointerSize;
            }

            MethodTable* interfaceType = ReadMethodTable(pointerTable + (nuint)descriptorIndex * (uint)pointerSize);
            ushort slot = *(ushort*)(slotTable + (nuint)descriptorIndex * sizeof(ushort));
            return CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, interfaceType, slot);
        }

        private static unsafe IntPtr ResolveGvmDispatch(TypeManagerHandle typeManager, object pObject, nuint cellIndex, uint cellCount)
        {
            if (cellIndex >= cellCount)
                throw new BadImageFormatException();

            byte* pInfo = (byte*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.GvmDispatchCellInfoRegion, out int length);
            int pointerSize = MethodTable.SupportsRelativePointers ? sizeof(int) : sizeof(nint);
            int descriptorSize = 2 * pointerSize + sizeof(int);
            byte* owningTypeTable;
            byte* instantiationTable;
            byte* flagsAndTokenTable;
            uint descriptorIndex;

            if ((ulong)length == (ulong)cellCount * (uint)descriptorSize)
            {
                owningTypeTable = pInfo;
                instantiationTable = pInfo + (nuint)cellCount * (uint)pointerSize;
                flagsAndTokenTable = instantiationTable + (nuint)cellCount * (uint)pointerSize;
                descriptorIndex = checked((uint)cellIndex);
            }
            else
            {
                GetDictionaryTables(pInfo, length, cellCount, pointerSize, descriptorSize, cellIndex,
                    out descriptorIndex, out uint descriptorCount, out owningTypeTable);
                instantiationTable = owningTypeTable + (nuint)descriptorCount * (uint)pointerSize;
                flagsAndTokenTable = instantiationTable + (nuint)descriptorCount * (uint)pointerSize;
            }

            MethodTable* owningType = ReadMethodTable(owningTypeTable + (nuint)descriptorIndex * (uint)pointerSize);
            void* instantiation = ReadPointer(instantiationTable + (nuint)descriptorIndex * (uint)pointerSize);
            int flagsAndToken = *(int*)(flagsAndTokenTable + (nuint)descriptorIndex * sizeof(int));

            return RuntimeAugments.TypeLoaderCallbacks.ResolveGenericVirtualMethodTarget(
                new RuntimeTypeHandle(pObject.GetMethodTable()),
                new RuntimeTypeHandle(owningType),
                new MethodHandle(flagsAndToken & ~GvmDispatchCellFlags.IsAsyncVariant),
                (flagsAndToken & GvmDispatchCellFlags.IsAsyncVariant) != 0,
                instantiation,
                isMethodInstantiationDataRelative: MethodTable.SupportsRelativePointers);
        }

        private static unsafe void GetDictionaryTables(
            byte* pInfo,
            int length,
            uint cellCount,
            int pointerSize,
            int descriptorSize,
            nuint cellIndex,
            out uint descriptorIndex,
            out uint descriptorCount,
            out byte* pointerTable)
        {
            // Dictionary format:
            // uint descriptorCount; index[cellCount]; padding; descriptor field arrays.
            if (length < sizeof(uint))
                throw new BadImageFormatException();

            descriptorCount = *(uint*)pInfo;
            if (descriptorCount == 0)
                throw new BadImageFormatException();

            int indexSize = GetIndexSize(descriptorCount);
            ulong pointerTableOffset = AlignUp(sizeof(uint) + (ulong)cellCount * (uint)indexSize, (uint)pointerSize);
            ulong expectedLength = pointerTableOffset + (ulong)descriptorCount * (uint)descriptorSize;
            if ((ulong)length != expectedLength || cellIndex >= cellCount)
                throw new BadImageFormatException();

            byte* pIndex = pInfo + sizeof(uint) + cellIndex * (uint)indexSize;
            descriptorIndex = indexSize switch
            {
                sizeof(byte) => *pIndex,
                sizeof(ushort) => *(ushort*)pIndex,
                _ => *(uint*)pIndex,
            };

            if (descriptorIndex >= descriptorCount)
                throw new BadImageFormatException();

            pointerTable = pInfo + pointerTableOffset;
        }

        private static int GetIndexSize(uint descriptorCount)
        {
            if (descriptorCount <= 1 << 8)
                return sizeof(byte);

            if (descriptorCount <= 1 << 16)
                return sizeof(ushort);

            return sizeof(uint);
        }

        private static ulong AlignUp(ulong value, uint alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        private static unsafe MethodTable* ReadMethodTable(byte* address)
        {
            return (MethodTable*)ReadPointer(address);
        }

        private static unsafe void* ReadPointer(byte* address)
        {
            if (MethodTable.SupportsRelativePointers)
                return address + *(int*)address;

            return *(void**)address;
        }
    }
}
