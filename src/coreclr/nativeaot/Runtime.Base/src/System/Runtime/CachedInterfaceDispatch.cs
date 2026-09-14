// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;

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
        }

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

        [RuntimeExport("RhpCidResolve")]
        private static IntPtr RhpCidResolve(IntPtr callerTransitionBlockParam, IntPtr pCell)
        {
            IntPtr locationOfThisPointer = callerTransitionBlockParam + TransitionBlock.GetThisOffset();
            object pObject = *(object*)locationOfThisPointer;
            IntPtr dispatchResolveTarget = RhpCidResolve_Worker(pObject, pCell);
            return dispatchResolveTarget;
        }

        [RuntimeExport("RhpCidResolve_Worker")]
        private static IntPtr RhpCidResolve_Worker(object pObject, IntPtr pCell)
        {
            var resolver = (delegate*<object, nint, nint>)InternalCalls.RhpGetClasslibFunctionFromEEType(pObject.GetMethodTable(), ClassLibFunctionId.ResolveDispatch);
            IntPtr pTargetCode = resolver(pObject, pCell);
            if (pTargetCode != IntPtr.Zero)
            {
                return UpdateDispatchCellCache(pCell, pTargetCode, pObject.GetMethodTable());
            }

            // "Valid method implementation was not found."
            EH.FallbackFailFast(RhFailFastReason.InternalError, null);
            return IntPtr.Zero;
        }

        private static IntPtr UpdateDispatchCellCache(IntPtr pCell, IntPtr pTargetCode, MethodTable* pInstanceType)
        {
            DispatchCell* pDispatchCell = (DispatchCell*)pCell;

            // If the dispatch cell doesn't cache anything yet, cache in the dispatch cell
            if (Interlocked.CompareExchange(ref pDispatchCell->Code, pTargetCode, 0) == 0)
            {
                // Use release semantics so the reader's acquire-load of MethodTable
                // guarantees the Code store is visible.
                Volatile.Write(ref pDispatchCell->MethodTable, (nint)pInstanceType);
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
            return RhResolveDispatchWorker(pObject, interfaceType, slot);
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

        internal static IntPtr RhResolveDispatchWorker(object pObject, MethodTable* pItfType, ushort itfSlotNumber)
        {
            // Type of object we're dispatching on.
            MethodTable* pInstanceType = pObject.GetMethodTable();
            IntPtr pTargetCode = DispatchResolve.FindInterfaceMethodImplementationTarget(pInstanceType,
                                                                            pItfType,
                                                                            itfSlotNumber,
                                                                            flags: default,
                                                                            ppGenericContext: null);
            if (DynamicInterfaceCastablePresent() && pTargetCode == IntPtr.Zero && pInstanceType->IsIDynamicInterfaceCastable)
            {
                // Dispatch not resolved through normal dispatch map, try using the IDynamicInterfaceCastable
                // This will either give us the appropriate result, or throw.
                pTargetCode = IDynamicInterfaceCastable.GetDynamicInterfaceImplementation((IDynamicInterfaceCastable)pObject, pItfType, itfSlotNumber);
                Diagnostics.Debug.Assert(pTargetCode != IntPtr.Zero);
            }
            return pTargetCode;
        }
    }
}
