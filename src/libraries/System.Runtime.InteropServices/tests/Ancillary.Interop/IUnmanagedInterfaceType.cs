// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// This interface allows another interface to define that it represents a managed projection of an unmanaged interface from some unmanaged type system.
    /// </summary>
    /// <typeparam name="TInterface">The managed interface.</typeparam>
    /// <typeparam name="TKey">The type of a value that can represent types from the corresponding unmanaged type system.</typeparam>
    public unsafe interface IUnmanagedInterfaceType
    {
        /// <summary>
        /// Get a pointer to the virtual method table of managed implementations of the unmanaged interface type.
        /// </summary>
        /// <returns>A pointer to the virtual method table of managed implementations of the unmanaged interface type</returns>
        /// <remarks>
        /// Implementation will be provided by a source generator if not explicitly implemented.
        /// This property can return <c>null</c>. If it does, then the interface is not supported for passing managed implementations to unmanaged code.
        /// </remarks>
        public abstract static void* VirtualMethodTableManagedImplementation { get; }
    }
}
