// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime;

namespace System.Runtime.InteropServices
{
    public unsafe partial interface IDynamicInterfaceCastable
    {
        internal static bool IsInterfaceImplemented(IDynamicInterfaceCastable instance, MethodTable* interfaceType, bool throwing)
        {
            return false;
        }

        internal static IntPtr GetDynamicInterfaceImplementation(IDynamicInterfaceCastable instance, MethodTable* interfaceType, ushort slot)
        {
            RuntimeImports.RhpFallbackFailFast();
            return default;
        }
    }
}
