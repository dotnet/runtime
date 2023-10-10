// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System.Runtime
{
    internal static unsafe class CachedInterfaceDispatch
    {
        [RuntimeExport("RhResolveDispatch")]
        private static IntPtr RhResolveDispatch(object pObject, EETypePtr interfaceType, ushort slot)
        {
            IntPtr pTargetCode = DispatchResolve.FindInterfaceMethodImplementationTarget(pObject.GetMethodTable(),
                                                                          interfaceType.ToPointer(),
                                                                          slot,
                                                                          ppGenericContext: null);
            if (pTargetCode == IntPtr.Zero && pObject.GetMethodTable()->IsIDynamicInterfaceCastable)
            {
                // Dispatch not resolved through normal dispatch map, try using the IDynamicInterfaceCastable
                // This will either give us the appropriate result, or throw.
                var pfnGetInterfaceImplementation = (delegate*<object, MethodTable*, ushort, IntPtr>)
                    pObject.GetMethodTable()->GetClasslibFunction(ClassLibFunctionId.IDynamicCastableGetInterfaceImplementation);
                pTargetCode = pfnGetInterfaceImplementation(pObject, interfaceType.ToPointer(), slot);
                Diagnostics.Debug.Assert(pTargetCode != IntPtr.Zero);
            }
            return pTargetCode;
        }

        [RuntimeExport("RhResolveDispatchOnType")]
        private static IntPtr RhResolveDispatchOnType(EETypePtr instanceType, EETypePtr interfaceType, ushort slot, EETypePtr* pGenericContext)
        {
            // Type of object we're dispatching on.
            MethodTable* pInstanceType = instanceType.ToPointer();

            // Type of interface
            MethodTable* pInterfaceType = interfaceType.ToPointer();

            return DispatchResolve.FindInterfaceMethodImplementationTarget(pInstanceType,
                                                                          pInterfaceType,
                                                                          slot,
                                                                          (MethodTable**)pGenericContext);
        }
    }
}
