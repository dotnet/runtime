// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.CompilerHelpers;

namespace System.Runtime
{
    // Initialize the cache eagerly to avoid null checks.
    [EagerStaticClassConstruction]
    internal static unsafe class CachedInterfaceDispatch
    {
#if SYSTEM_PRIVATE_CORELIB
#if DEBUG
        // use smaller numbers to hit resizing/preempting logic in debug
        private const int InitialCacheSize = 8; // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 512;
#else
        private const int InitialCacheSize = 128; // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 128 * 1024;
#endif // DEBUG

        private static GenericCache<Key, nint> s_cache
            = new GenericCache<Key, nint>(InitialCacheSize, MaximumCacheSize);

        static CachedInterfaceDispatch()
        {
            RuntimeImports.RhpRegisterDispatchCache(ref Unsafe.As<GenericCache<Key, nint>, byte>(ref s_cache));

#if false
            Lookup1234(new object(), 0x123456, ref s_cache);
#endif
        }

#if false
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static nint Lookup1234(object thisobj, nint cell, ref GenericCache<Key, nint> cache)
        {
            if (cache.TryGet(new Key(cell, (nint)thisobj.GetMethodTable()), out nint result))
                return result;

            return SlowPath(thisobj, cell);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static nint SlowPath(object thisobj, nint cell)
        {
            return 0;
        }
#endif

        private struct Key : IEquatable<Key>
        {
            public IntPtr _dispatchCell;
            public IntPtr _objectType;

            public Key(nint dispatchCell, nint objectType)
            {
                _dispatchCell = dispatchCell;
                _objectType = objectType;
            }

            public bool Equals(Key other)
            {
                return _dispatchCell == other._dispatchCell && _objectType == other._objectType;
            }

            public override int GetHashCode()
            {
                // pointers will likely match and cancel out in the upper bits
                // we will rotate context by 16 bit to keep more varying bits in the hash
                IntPtr context = (IntPtr)System.Numerics.BitOperations.RotateLeft((nuint)_dispatchCell, 16);
                return (context ^ _objectType).GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is Key && Equals((Key)obj);
            }
        }
#endif

        [StructLayout(LayoutKind.Sequential)]
        private struct DispatchCell
        {
            public nint MethodTable;
            public nint Code;
        }

        [RuntimeExport("RhpCidResolve")]
        private static unsafe IntPtr RhpCidResolve(IntPtr callerTransitionBlockParam, IntPtr pCell)
        {
            IntPtr locationOfThisPointer = callerTransitionBlockParam + TransitionBlock.GetThisOffset();
            object pObject = *(object*)locationOfThisPointer;
            IntPtr dispatchResolveTarget = RhpCidResolve_Worker(pObject, pCell);
            return dispatchResolveTarget;
        }

        private static IntPtr RhpCidResolve_Worker(object pObject, IntPtr pCell)
        {
            DispatchCellInfo cellInfo;
            GetDispatchCellInfo(pObject.GetMethodTable()->TypeManager, pCell, out cellInfo);

            IntPtr pTargetCode = RhResolveDispatchWorker(pObject, (void*)pCell, ref cellInfo);
            if (pTargetCode != IntPtr.Zero)
            {
                return UpdateDispatchCellCache(pCell, pTargetCode, pObject.GetMethodTable());
            }

            // "Valid method implementation was not found."
            EH.FallbackFailFast(RhFailFastReason.InternalError, null);
            return IntPtr.Zero;
        }

        private static void GetDispatchCellInfo(TypeManagerHandle typeManager, IntPtr pCell, out DispatchCellInfo info)
        {
            IntPtr dispatchCellRegion = RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.InterfaceDispatchCellRegion, out int length);
            if (pCell >= dispatchCellRegion && pCell < dispatchCellRegion + length)
            {
                // Static dispatch cell: find the info in the associated info region
                nint cellIndex = (pCell - dispatchCellRegion) / sizeof(DispatchCell);

                IntPtr dispatchCellInfoRegion = RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.InterfaceDispatchCellInfoRegion, out _);
                if (MethodTable.SupportsRelativePointers)
                {
                    var dispatchCellInfo = (int*)dispatchCellInfoRegion;
                    info = new DispatchCellInfo
                    {
                        CellType = DispatchCellType.InterfaceAndSlot,
                        InterfaceType = (MethodTable*)ReadRelPtr32(dispatchCellInfo + (cellIndex * 2)),
                        InterfaceSlot = (ushort)*(dispatchCellInfo + (cellIndex * 2 + 1))
                    };

                    static void* ReadRelPtr32(void* address)
                        => (byte*)address + *(int*)address;
                }
                else
                {
                    var dispatchCellInfo = (nint*)dispatchCellInfoRegion;
                    info = new DispatchCellInfo
                    {
                        CellType = DispatchCellType.InterfaceAndSlot,
                        InterfaceType = (MethodTable*)(dispatchCellInfo + (cellIndex * 2)),
                        InterfaceSlot = (ushort)*(dispatchCellInfo + (cellIndex * 2 + 1))
                    };
                }

            }
            else
            {
                // Dynamically allocated dispatch cell: info is next to the dispatch cell
                info = new DispatchCellInfo
                {
                    CellType = DispatchCellType.InterfaceAndSlot,
                    InterfaceType = *(MethodTable**)(pCell + sizeof(DispatchCell)),
                    InterfaceSlot = (ushort)*(nint*)(pCell + sizeof(DispatchCell) + sizeof(MethodTable*))
                };
            }
        }

        private static IntPtr UpdateDispatchCellCache(IntPtr pCell, IntPtr pTargetCode, MethodTable* pInstanceType)
        {
            DispatchCell* pDispatchCell = (DispatchCell*)pCell;

            // If the dispatch cell doesn't cache anything yet, cache in the dispatch cell
            if (Interlocked.CompareExchange(ref pDispatchCell->MethodTable, (nint)pInstanceType, 0) == 0)
            {
                // TODO: Michal doing lockfree code danger
                pDispatchCell->Code = pTargetCode;
            }
            else
            {
                // Otherwise cache in the hashtable
#if SYSTEM_PRIVATE_CORELIB
                s_cache.TrySet(new Key(pCell, (nint)pInstanceType), pTargetCode);
#endif
            }

            return pTargetCode;
        }

        [RuntimeExport("RhpResolveInterfaceMethod")]
        private static IntPtr RhpResolveInterfaceMethod(object pObject, IntPtr pCell)
        {
            if (pObject == null)
            {
                // Optimizer may perform code motion on dispatch such that it occurs independent of
                // null check on "this" pointer. Allow for this case by returning back an invalid pointer.
                return IntPtr.Zero;
            }

            MethodTable* pInstanceType = pObject.GetMethodTable();

            // This method is used for the implementation of LOAD_VIRT_FUNCTION and in that case the mapping we want
            // may already be in the cache.
            IntPtr pTargetCode = 0;
            var dispatchCell = (DispatchCell*)pCell;
            if (dispatchCell->Code != 0)
            {
                if ((MethodTable*)dispatchCell->MethodTable == pInstanceType)
                {
                    pTargetCode = dispatchCell->Code;
                }
                else
                {
#if SYSTEM_PRIVATE_CORELIB
                    if (!s_cache.TryGet(new Key(pCell, (nint)pInstanceType), out pTargetCode))
                    {
                        pTargetCode = 0;
                    }
#endif
                }
            }

            if (pTargetCode == IntPtr.Zero)
            {
                // Otherwise call the version of this method that knows how to resolve the method manually.
                pTargetCode = RhpCidResolve_Worker(pObject, pCell);
            }

            return pTargetCode;
        }

        [RuntimeExport("RhResolveDispatch")]
        private static IntPtr RhResolveDispatch(object pObject, MethodTable* interfaceType, ushort slot)
        {
            DispatchCellInfo cellInfo = default;
            cellInfo.CellType = DispatchCellType.InterfaceAndSlot;
            cellInfo.InterfaceType = interfaceType;
            cellInfo.InterfaceSlot = slot;

            return RhResolveDispatchWorker(pObject, null, ref cellInfo);
        }

        [RuntimeExport("RhResolveDispatchOnType")]
        private static IntPtr RhResolveDispatchOnType(MethodTable* pInstanceType, MethodTable* pInterfaceType, ushort slot)
        {
            return DispatchResolve.FindInterfaceMethodImplementationTarget(pInstanceType,
                                                                          pInterfaceType,
                                                                          slot,
                                                                          flags: default,
                                                                          ppGenericContext: null);
        }

        [RuntimeExport("RhResolveStaticDispatchOnType")]
        private static IntPtr RhResolveStaticDispatchOnType(MethodTable* pInstanceType, MethodTable* pInterfaceType, ushort slot, MethodTable** ppGenericContext)
        {
            return DispatchResolve.FindInterfaceMethodImplementationTarget(pInstanceType,
                                                                          pInterfaceType,
                                                                          slot,
                                                                          DispatchResolve.ResolveFlags.Static,
                                                                          ppGenericContext);
        }

        [RuntimeExport("RhResolveDynamicInterfaceCastableDispatchOnType")]
        private static IntPtr RhResolveDynamicInterfaceCastableDispatchOnType(MethodTable* pInstanceType, MethodTable* pInterfaceType, ushort slot, MethodTable** ppGenericContext)
        {
            IntPtr result =  DispatchResolve.FindInterfaceMethodImplementationTarget(pInstanceType,
                                                                                    pInterfaceType,
                                                                                    slot,
                                                                                    DispatchResolve.ResolveFlags.IDynamicInterfaceCastable,
                                                                                    ppGenericContext);

            if ((result & (nint)DispatchMapCodePointerFlags.RequiresInstantiatingThunkFlag) != 0)
            {
                result &= ~(nint)DispatchMapCodePointerFlags.RequiresInstantiatingThunkFlag;
            }
            else
            {
                *ppGenericContext = null;
            }

            return result;
        }

        [Intrinsic]
        [AnalysisCharacteristic]
        private static extern bool DynamicInterfaceCastablePresent();

        private static unsafe IntPtr RhResolveDispatchWorker(object pObject, void* cell, ref DispatchCellInfo cellInfo)
        {
            // Type of object we're dispatching on.
            MethodTable* pInstanceType = pObject.GetMethodTable();

            if (cellInfo.CellType == DispatchCellType.InterfaceAndSlot)
            {
                IntPtr pTargetCode = DispatchResolve.FindInterfaceMethodImplementationTarget(pInstanceType,
                                                                              cellInfo.InterfaceType,
                                                                              cellInfo.InterfaceSlot,
                                                                              flags: default,
                                                                              ppGenericContext: null);
                if (DynamicInterfaceCastablePresent() && pTargetCode == IntPtr.Zero && pInstanceType->IsIDynamicInterfaceCastable)
                {
                    // Dispatch not resolved through normal dispatch map, try using the IDynamicInterfaceCastable
                    // This will either give us the appropriate result, or throw.
                    pTargetCode = IDynamicInterfaceCastable.GetDynamicInterfaceImplementation((IDynamicInterfaceCastable)pObject, cellInfo.InterfaceType, cellInfo.InterfaceSlot);
                    Diagnostics.Debug.Assert(pTargetCode != IntPtr.Zero);
                }
                return pTargetCode;
            }
            else if (cellInfo.CellType == DispatchCellType.VTableOffset)
            {
                // Dereference VTable
                return *(IntPtr*)(((byte*)pInstanceType) + cellInfo.VTableOffset);
            }
            else
            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING_AND_SUPPORTS_TOKEN_BASED_DISPATCH_CELLS
                // Attempt to convert dispatch cell to non-metadata form if we haven't acquired a cache for this cell yet
                if (cellInfo.HasCache == 0)
                {
                    cellInfo = InternalTypeLoaderCalls.ConvertMetadataTokenDispatch(InternalCalls.RhGetModuleFromPointer(cell), cellInfo);
                    if (cellInfo.CellType != DispatchCellType.MetadataToken)
                    {
                        return RhResolveDispatchWorker(pObject, cell, ref cellInfo);
                    }
                }

                // If that failed, go down the metadata resolution path
                return InternalTypeLoaderCalls.ResolveMetadataTokenDispatch(InternalCalls.RhGetModuleFromPointer(cell), (int)cellInfo.MetadataToken, new IntPtr(pInstanceType));
#else
                EH.FallbackFailFast(RhFailFastReason.InternalError, null);
                return IntPtr.Zero;
#endif
            }
        }
    }
}
