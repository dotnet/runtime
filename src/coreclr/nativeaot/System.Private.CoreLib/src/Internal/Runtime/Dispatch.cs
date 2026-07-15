// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
                    return ResolveStaticInterfaceDispatch(typeManager, pObject, (nint)(pCell - pDispatchCellRegion));
                }

                pDispatchCellRegion = (DispatchCell*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.GvmDispatchCellRegion, out length);
                if ((byte*)pCell >= (byte*)pDispatchCellRegion && (byte*)pCell < (byte*)pDispatchCellRegion + length)
                {
                    return ResolveGvmDispatch(typeManager, pObject, (nint)(pCell - pDispatchCellRegion));
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

        private static unsafe IntPtr ResolveStaticInterfaceDispatch(TypeManagerHandle typeManager, object pObject, nint cellIndex)
        {
            IntPtr pDispatchCellInfoRegion = RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.InterfaceDispatchCellInfoRegion, out _);
            if (MethodTable.SupportsRelativePointers)
            {
                var dispatchCellInfo = &((RelativeInterfaceDispatchInfo*)pDispatchCellInfoRegion)[cellIndex];
                return CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, dispatchCellInfo->InterfaceType, (ushort)dispatchCellInfo->Slot);
            }
            else
            {
                var dispatchCellInfo = &((InterfaceDispatchInfo*)pDispatchCellInfoRegion)[cellIndex];
                return CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, dispatchCellInfo->InterfaceType, (ushort)dispatchCellInfo->Slot);
            }
        }

        private static unsafe IntPtr ResolveGvmDispatch(TypeManagerHandle typeManager, object pObject, nint cellIndex)
        {
            IntPtr pDispatchCellInfoRegion = RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.GvmDispatchCellInfoRegion, out _);
            if (MethodTable.SupportsRelativePointers)
            {
                var dispatchCellInfo = &((RelativeGvmDispatchInfo*)pDispatchCellInfoRegion)[cellIndex];
                return RuntimeAugments.TypeLoaderCallbacks.ResolveGenericVirtualMethodTarget(
                    new RuntimeTypeHandle(pObject.GetMethodTable()), new RuntimeTypeHandle(dispatchCellInfo->OwningType), dispatchCellInfo->Handle, dispatchCellInfo->IsAsyncVariant, dispatchCellInfo->Instantiation, isMethodInstantiationDataRelative: true);
            }
            else
            {
                var dispatchCellInfo = &((GvmDispatchInfo*)pDispatchCellInfoRegion)[cellIndex];
                return RuntimeAugments.TypeLoaderCallbacks.ResolveGenericVirtualMethodTarget(
                    new RuntimeTypeHandle(pObject.GetMethodTable()), new RuntimeTypeHandle(dispatchCellInfo->OwningType), dispatchCellInfo->Handle, dispatchCellInfo->IsAsyncVariant, dispatchCellInfo->Instantiation, isMethodInstantiationDataRelative: false);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RelativeInterfaceDispatchInfo
        {
            private int _interfaceTypeRelPtr;
            public int Slot;

            public unsafe MethodTable* InterfaceType
                => (MethodTable*)((byte*)Unsafe.AsPointer(ref _interfaceTypeRelPtr) + _interfaceTypeRelPtr);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct InterfaceDispatchInfo
        {
            public unsafe MethodTable* InterfaceType;
            private nint _slot;

            public int Slot => (int)_slot;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RelativeGvmDispatchInfo
        {
            private int _owningTypeRelPtr;
            private int _compositionRelPtr;
            private int _flagsAndToken;

            public unsafe MethodTable* OwningType
                => (MethodTable*)((byte*)Unsafe.AsPointer(ref _owningTypeRelPtr) + _owningTypeRelPtr);

            public unsafe void* Instantiation
                => (byte*)Unsafe.AsPointer(ref _compositionRelPtr) + _compositionRelPtr;

            public MethodHandle Handle => new MethodHandle(_flagsAndToken & ~GvmDispatchCellFlags.IsAsyncVariant);

            public bool IsAsyncVariant => (_flagsAndToken & GvmDispatchCellFlags.IsAsyncVariant) != 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GvmDispatchInfo
        {
            public unsafe MethodTable* OwningType;
            public unsafe void* Instantiation;
            private nint _flagsAndToken;

            public MethodHandle Handle => new MethodHandle((int)_flagsAndToken & ~GvmDispatchCellFlags.IsAsyncVariant);

            public bool IsAsyncVariant => ((int)_flagsAndToken & GvmDispatchCellFlags.IsAsyncVariant) != 0;
        }
    }
}
