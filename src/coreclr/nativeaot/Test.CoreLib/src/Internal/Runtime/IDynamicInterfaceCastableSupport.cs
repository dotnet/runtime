// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

namespace Internal.Runtime
{
    internal static unsafe class IDynamicCastableSupport
    {
        internal static IntPtr IDynamicCastableGetInterfaceImplementation(object instance, MethodTable* interfaceType, ushort slot)
        {
            RuntimeImports.RhpFallbackFailFast();
            return default;
        }
    }
}
