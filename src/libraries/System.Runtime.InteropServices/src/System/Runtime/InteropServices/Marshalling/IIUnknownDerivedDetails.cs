// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM source generator and implements part of the COM-specific interactions.
// This API need to be exposed to implement the COM source generator in one form or another.

using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Details for the IUnknown derived interface.
    /// </summary>
    [CLSCompliant(false)]
    public interface IIUnknownDerivedDetails
    {
        /// <summary>
        /// Interface ID.
        /// </summary>
        public Guid Iid { get; }

        /// <summary>
        /// Managed type used to project the IUnknown derived interface.
        /// </summary>
        public Type Implementation { get; }

        /// <summary>
        /// A pointer to the virtual method table to enable unmanaged callers to call a managed implementation of the interface.
        /// </summary>
        public unsafe void** ManagedVirtualMethodTable { get; }

        internal static IIUnknownDerivedDetails? GetFromAttribute(RuntimeTypeHandle handle)
        {
            var type = Type.GetTypeFromHandle(handle);
            if (type is null)
            {
                return null;
            }
            return (IIUnknownDerivedDetails?)type.GetCustomAttribute(typeof(IUnknownDerivedAttribute<,>));
        }
    }
}
