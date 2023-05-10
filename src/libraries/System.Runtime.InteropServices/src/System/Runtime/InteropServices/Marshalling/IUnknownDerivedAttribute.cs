// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM source generator and implements part of the COM-specific interactions.
// This API need to be exposed to implement the COM source generator in one form or another.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// An attribute to mark this interface as a managed representation of an IUnknown-derived interface.
    /// </summary>
    /// <typeparam name="T">The type that provides type-level information about the interface.</typeparam>
    /// <typeparam name="TImpl">The type to use for calling from managed callers to unmanaged implementations of the interface.</typeparam>
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    [CLSCompliant(false)]
    public class IUnknownDerivedAttribute<T, TImpl> : Attribute, IIUnknownDerivedDetails
        where T : IIUnknownInterfaceType
    {
        public IUnknownDerivedAttribute()
        {
        }

        /// <inheritdoc />
        public Guid Iid => T.Iid;

        /// <inheritdoc />
        public Type Implementation => typeof(TImpl);

        /// <inheritdoc />
        public unsafe void** ManagedVirtualMethodTable => T.ManagedVirtualMethodTable;
    }
}
