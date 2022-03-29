// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

namespace Internal.Runtime
{
    static unsafe class IDynamicCastableSupport
    {
        [RuntimeExport("IDynamicCastableIsInterfaceImplemented")]
        internal static bool IDynamicCastableIsInterfaceImplemented(object instance, MethodTable* interfaceType, bool throwIfNotImplemented)
        {
            return false;
        }

        [RuntimeExport("IDynamicCastableGetInterfaceImplementation")]
        internal static IntPtr IDynamicCastableGetInterfaceImplementation(object instance, MethodTable* interfaceType, ushort slot)
        {
            RuntimeImports.RhpFallbackFailFast();
            return default;
        }
    }
}