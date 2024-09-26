// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime.Augments;
using Internal.TypeSystem;

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

        private static readonly object s_thunkPoolHeap = RuntimeAugments.CreateThunksHeap(RuntimeImports.GetInteropCommonStubAddress());

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
                if (!s_thunkHashtable.TryGetValue(new InstantiatingThunkKey(result, (nint)genericContext), out nint thunk))
                {
                    thunk = RuntimeAugments.AllocateThunk(s_thunkPoolHeap);
                    RuntimeAugments.SetThunkData(s_thunkPoolHeap, thunk, (nint)genericContext, result);
                    nint thunkInHashtable = s_thunkHashtable.AddOrGetExisting(thunk);
                    if (thunkInHashtable != thunk)
                    {
                        RuntimeAugments.FreeThunk(s_thunkPoolHeap, thunk);
                        thunk = thunkInHashtable;
                    }
                }

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

        private static readonly InstantiatingThunkHashtable s_thunkHashtable = new InstantiatingThunkHashtable();

        private class InstantiatingThunkHashtable : LockFreeReaderHashtableOfPointers<InstantiatingThunkKey, nint>
        {
            protected override bool CompareKeyToValue(InstantiatingThunkKey key, nint value)
            {
                bool result = RuntimeAugments.TryGetThunkData(s_thunkPoolHeap, value, out nint context, out nint target);
                Debug.Assert(result);
                return key.Target == target && key.Context == context;
            }

            protected override bool CompareValueToValue(nint value1, nint value2)
            {
                bool result1 = RuntimeAugments.TryGetThunkData(s_thunkPoolHeap, value1, out nint context1, out nint target1);
                Debug.Assert(result1);

                bool result2 = RuntimeAugments.TryGetThunkData(s_thunkPoolHeap, value2, out nint context2, out nint target2);
                Debug.Assert(result2);
                return context1 == context2 && target1 == target2;
            }

            protected override nint ConvertIntPtrToValue(nint pointer) => pointer;
            protected override nint ConvertValueToIntPtr(nint value) => value;
            protected override nint CreateValueFromKey(InstantiatingThunkKey key) => throw new NotImplementedException();
            protected override int GetKeyHashCode(InstantiatingThunkKey key) => HashCode.Combine(key.Target, key.Context);

            protected override int GetValueHashCode(nint value)
            {
                bool result = RuntimeAugments.TryGetThunkData(s_thunkPoolHeap, value, out nint context, out nint target);
                Debug.Assert(result);
                return HashCode.Combine(target, context);
            }
        }

        private struct InstantiatingThunkKey
        {
            public readonly nint Target;
            public readonly nint Context;
            public InstantiatingThunkKey(nint target, nint context) => (Target, Context) = (target, context);
        }
    }
}
