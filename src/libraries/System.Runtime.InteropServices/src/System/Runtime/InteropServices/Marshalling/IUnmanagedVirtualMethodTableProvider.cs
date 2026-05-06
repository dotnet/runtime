// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM Source Generator and defines basic vtable interactions that we would need in the COM source generator in one form or another.
namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// This interface allows an object to provide information about a virtual method table for a managed interface to enable invoking methods in the virtual method table.
    /// </summary>
    [CLSCompliant(false)]
    public unsafe interface IUnmanagedVirtualMethodTableProvider
    {
        /// <summary>
        /// Get the information about the virtual method table for a given unmanaged interface type represented by <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The managed type for the unmanaged interface.</param>
        /// <returns>The virtual method table information for the unmanaged interface.</returns>
        public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type);
    }
}
