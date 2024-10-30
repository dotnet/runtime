// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to implement shared generic code.
    /// </summary>
    internal static class SharedCodeHelpers
    {
        public static unsafe MethodTable* GetOrdinalInterface(MethodTable* pType, ushort interfaceIndex)
        {
            Debug.Assert(interfaceIndex < pType->NumInterfaces);
            return pType->InterfaceMap[interfaceIndex];
        }

        public static unsafe MethodTable* GetCurrentSharedThunkContext()
        {
            return (MethodTable*)RuntimeImports.GetCurrentInteropThunkContext();
        }
    }
}
