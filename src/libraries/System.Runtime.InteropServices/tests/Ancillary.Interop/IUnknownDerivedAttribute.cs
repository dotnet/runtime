// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM source generator and implements part of the COM-specific interactions.
// This API need to be exposed to implement the COM source generator in one form or another.

namespace System.Runtime.InteropServices.Marshalling
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class IUnknownDerivedAttribute<T, TImpl> : Attribute, IUnknownDerivedDetails
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
