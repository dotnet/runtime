// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime.Augments;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime
{
    internal static unsafe class IDynamicCastableSupport
    {
        [RuntimeExport("IDynamicCastableIsInterfaceImplemented")]
        internal static bool IDynamicCastableIsInterfaceImplemented(IDynamicInterfaceCastable instance, MethodTable* interfaceType, bool throwIfNotImplemented)
        {
            return instance.IsInterfaceImplemented(new RuntimeTypeHandle(interfaceType), throwIfNotImplemented);
        }

        private static object s_thunkPoolHeap;

        [RuntimeExport("IDynamicCastableGetInterfaceImplementation")]
        internal static IntPtr IDynamicCastableGetInterfaceImplementation(IDynamicInterfaceCastable instance, MethodTable* interfaceType, ushort slot)
        {
            RuntimeTypeHandle handle = instance.GetInterfaceImplementation(new RuntimeTypeHandle(interfaceType));
            MethodTable* implType = handle.ToMethodTable();
            if (implType == null)
            {
                ThrowInvalidCastException(instance, interfaceType);
            }
            if (!implType->IsInterface)
            {
                ThrowInvalidOperationException(implType);
            }

            MethodTable* genericContext = null;
            IntPtr result = RuntimeImports.RhResolveDynamicInterfaceCastableDispatchOnType(implType, interfaceType, slot, &genericContext);
            if (result == IntPtr.Zero)
            {
                IDynamicCastableGetInterfaceImplementationFailure(instance, interfaceType, implType);
            }

            if (genericContext != null)
            {
                if (s_thunkPoolHeap == null)
                {
                    // TODO: Free s_thunkPoolHeap if the thread lose the race
                    Interlocked.CompareExchange(
                        ref s_thunkPoolHeap,
                        RuntimeAugments.CreateThunksHeap(RuntimeImports.GetInteropCommonStubAddress()),
                        null
                    );
                    Debug.Assert(s_thunkPoolHeap != null);
                }

                nint thunk = RuntimeAugments.AllocateThunk(s_thunkPoolHeap);
                RuntimeAugments.SetThunkData(s_thunkPoolHeap, thunk, (nint)genericContext, result);

                result = thunk;
            }
            return result;
        }

        private static void ThrowInvalidCastException(object instance, MethodTable* interfaceType)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, instance.GetType(), Type.GetTypeFromMethodTable(interfaceType)));
        }

        private static void ThrowInvalidOperationException(MethodTable* resolvedImplType)
        {
            throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_NotInterface, Type.GetTypeFromMethodTable(resolvedImplType)));
        }

        private static void IDynamicCastableGetInterfaceImplementationFailure(object instance, MethodTable* interfaceType, MethodTable* resolvedImplType)
        {
            if (resolvedImplType->DispatchMap == null)
                throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_MissingImplementationAttribute, Type.GetTypeFromMethodTable(resolvedImplType), nameof(DynamicInterfaceCastableImplementationAttribute)));

            bool implementsInterface = false;
            var interfaces = resolvedImplType->InterfaceMap;
            for (int i = 0; i < resolvedImplType->NumInterfaces; i++)
            {
                if (interfaces[i] == interfaceType)
                {
                    implementsInterface = true;
                    break;
                }
            }

            if (!implementsInterface)
                throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_DoesNotImplementRequested, Type.GetTypeFromMethodTable(resolvedImplType), Type.GetTypeFromMethodTable(interfaceType)));

            throw new EntryPointNotFoundException();
        }
    }
}
