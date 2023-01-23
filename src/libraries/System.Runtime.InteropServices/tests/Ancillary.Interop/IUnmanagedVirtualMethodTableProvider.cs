// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// This interface allows another interface to define that it represents a managed projection of an unmanaged interface from some unmanaged type system.
    /// </summary>
    /// <typeparam name="TInterface">The managed interface.</typeparam>
    /// <typeparam name="TKey">The type of a value that can represent types from the corresponding unmanaged type system.</typeparam>
    public unsafe interface IUnmanagedInterfaceType<TInterface>
        where TInterface : IUnmanagedInterfaceType<TInterface>
    {
        /// <summary>
        /// Get the length of the virtual method table for the given unmanaged interface type.
        /// </summary>
        /// <returns>The length of the virtual method table for the unmanaged interface.</returns>
        public static abstract int VirtualMethodTableLength { get; }

        /// <summary>
        /// Get a pointer to the virtual method table  of managed implementations of the unmanaged interface type.
        /// </summary>
        /// <returns>A pointer to the virtual method table  of managed implementations of the unmanaged interface type</returns>
        public static abstract void* VirtualMethodTableManagedImplementation { get; }

        /// <summary>
        /// Get a pointer that wraps a managed implementation of an unmanaged interface that can be passed to unmanaged code.
        /// </summary>
        /// <param name="obj">The managed object that implements the unmanaged interface.</param>
        /// <returns>A pointer-sized value that can be passed to unmanaged code that represents <paramref name="obj"/></returns>
        public static abstract void* GetUnmanagedWrapperForObject(TInterface obj);

        /// <summary>
        /// Get the object wrapped by <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr">A pointer-sized value returned by <see cref="IUnmanagedVirtualMethodTableProvider{TKey}.GetUnmanagedWrapperForObject{IUnmanagedInterfaceType{TInterface, TKey}}(IUnmanagedInterfaceType{TInterface, TKey})"/> or <see cref="GetUnmanagedWrapperForObject(TInterface)"/>.</param>
        /// <returns>The object wrapped by <paramref name="ptr"/>.</returns>
        public static abstract TInterface GetObjectForUnmanagedWrapper(void* ptr);
    }
}
