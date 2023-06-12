// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM source generator and implements part of the COM-specific interactions.
// This API need to be exposed to implement the COM source generator in one form or another.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Type level information for an IUnknown-derived interface.
    /// </summary>
    [CLSCompliant(false)]
    public unsafe interface IIUnknownInterfaceType
    {
        /// <summary>
        /// The Interface ID (IID) for the interface.
        /// </summary>
        static abstract Guid Iid { get; }

        /// <summary>
        /// A pointer to the virtual method table to enable unmanaged callers to call a managed implementation of the interface.
        /// </summary>
        static abstract void** ManagedVirtualMethodTable { get; }
    }
}
