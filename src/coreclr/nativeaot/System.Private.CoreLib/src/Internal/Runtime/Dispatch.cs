// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerHelpers;

namespace Internal.Runtime
{
    internal static class Dispatch
    {
        [RuntimeExport("ResolveDispatch")]
        internal static unsafe MethodTable* ResolveDispatch(object pObject, DispatchCell* pCell)
        {
            // Assume this is a static dispatch cell first
            foreach (TypeManagerHandle typeManager in StartupCodeHelpers.GetLoadedModules())
            {
                var pDispatchCellRegion = (DispatchCell*)RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.InterfaceDispatchCellRegion, out int length);
                if (pCell >= pDispatchCellRegion && pCell < (byte*)pDispatchCellRegion + length)
                {
                    return ResolveStaticInterfaceDispatch(typeManager, pObject, (nint)(pCell - pDispatchCellRegion));
                }
            }

            // Not found statically: must be a dynamic dispatch cell
            var pDynamicCell = (DynamicDispatchCell*)pCell;
            return (MethodTable*)CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, pDynamicCell->InterfaceType, (ushort)pDynamicCell->Slot);
        }

        private static unsafe MethodTable* ResolveStaticInterfaceDispatch(TypeManagerHandle typeManager, object pObject, nint cellIndex)
        {
            IntPtr pDispatchCellInfoRegion = RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.InterfaceDispatchCellInfoRegion, out _);
            if (MethodTable.SupportsRelativePointers)
            {
                var dispatchCellInfo = &((RelativeInterfaceDispatchInfo*)pDispatchCellInfoRegion)[cellIndex];
                return (MethodTable*)CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, dispatchCellInfo->InterfaceType, (ushort)dispatchCellInfo->Slot);
            }
            else
            {
                var dispatchCellInfo = &((InterfaceDispatchInfo*)pDispatchCellInfoRegion)[cellIndex];
                return (MethodTable*)CachedInterfaceDispatch.RhResolveDispatchWorker(pObject, dispatchCellInfo->InterfaceType, (ushort)dispatchCellInfo->Slot);
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
    }
}
