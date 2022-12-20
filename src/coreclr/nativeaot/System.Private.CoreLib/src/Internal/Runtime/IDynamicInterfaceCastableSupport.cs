// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Internal.Runtime
{
    static unsafe class IDynamicCastableSupport
    {
        [RuntimeExport("IDynamicCastableIsInterfaceImplemented")]
        internal static bool IDynamicCastableIsInterfaceImplemented(IDynamicInterfaceCastable instance, MethodTable* interfaceType, bool throwIfNotImplemented)
        {
            return instance.IsInterfaceImplemented(new RuntimeTypeHandle(new EETypePtr(interfaceType)), throwIfNotImplemented);
        }

        [RuntimeExport("IDynamicCastableGetInterfaceImplementation")]
        internal static IntPtr IDynamicCastableGetInterfaceImplementation(IDynamicInterfaceCastable instance, MethodTable* interfaceType, ushort slot)
        {
            RuntimeTypeHandle handle = instance.GetInterfaceImplementation(new RuntimeTypeHandle(new EETypePtr(interfaceType)));
            EETypePtr implType = handle.ToEETypePtr();
            if (implType.IsNull)
            {
                ThrowInvalidCastException(instance, interfaceType);
            }
            if (!implType.IsInterface)
            {
                ThrowInvalidOperationException(implType);
            }
            IntPtr result = RuntimeImports.RhResolveDispatchOnType(implType, new EETypePtr(interfaceType), slot);
            if (result == IntPtr.Zero)
            {
                IDynamicCastableGetInterfaceImplementationFailure(instance, interfaceType, implType);
            }
            return result;
        }

        private static void ThrowInvalidCastException(object instance, MethodTable* interfaceType)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, instance.GetType(), Type.GetTypeFromEETypePtr(new EETypePtr(interfaceType))));
        }

        private static void ThrowInvalidOperationException(EETypePtr resolvedImplType)
        {
            throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_NotInterface, Type.GetTypeFromEETypePtr(resolvedImplType)));
        }

        private static void IDynamicCastableGetInterfaceImplementationFailure(object instance, MethodTable* interfaceType, EETypePtr resolvedImplType)
        {
            if (resolvedImplType.DispatchMap == IntPtr.Zero)
                throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_MissingImplementationAttribute, Type.GetTypeFromEETypePtr(resolvedImplType), nameof(DynamicInterfaceCastableImplementationAttribute)));

            bool implementsInterface = false;
            var interfaces = resolvedImplType.Interfaces;
            for (int i = 0; i < interfaces.Count; i++)
            {
                if (interfaces[i] == new EETypePtr(interfaceType))
                {
                    implementsInterface = true;
                    break;
                }
            }

            if (!implementsInterface)
                throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_DoesNotImplementRequested, Type.GetTypeFromEETypePtr(resolvedImplType), Type.GetTypeFromEETypePtr(new EETypePtr(interfaceType))));

            throw new EntryPointNotFoundException();
        }
    }
}
