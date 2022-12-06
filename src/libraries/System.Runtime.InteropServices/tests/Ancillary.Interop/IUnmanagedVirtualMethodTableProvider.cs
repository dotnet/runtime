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
    /// Information about a virtual method table and the unmanaged instance pointer.
    /// </summary>
    public readonly ref struct VirtualMethodTableInfo
    {
        /// <summary>
        /// Construct a <see cref="VirtualMethodTableInfo"/> from a given instance pointer and table memory.
        /// </summary>
        /// <param name="thisPointer">The pointer to the instance.</param>
        /// <param name="virtualMethodTable">The block of memory that represents the virtual method table.</param>
        public VirtualMethodTableInfo(IntPtr thisPointer, ReadOnlySpan<IntPtr> virtualMethodTable)
        {
            ThisPointer = thisPointer;
            VirtualMethodTable = virtualMethodTable;
        }

        /// <summary>
        /// The unmanaged instance pointer
        /// </summary>
        public IntPtr ThisPointer { get; }

        /// <summary>
        /// The virtual method table.
        /// </summary>
        public ReadOnlySpan<IntPtr> VirtualMethodTable { get; }

        /// <summary>
        /// Deconstruct this structure into its two fields.
        /// </summary>
        /// <param name="thisPointer">The <see cref="ThisPointer"/> result</param>
        /// <param name="virtualMethodTable">The <see cref="VirtualMethodTable"/> result</param>
        public void Deconstruct(out IntPtr thisPointer, out ReadOnlySpan<IntPtr> virtualMethodTable)
        {
            thisPointer = ThisPointer;
            virtualMethodTable = VirtualMethodTable;
        }
    }

    /// <summary>
    /// This interface allows an object to provide information about a virtual method table for a managed interface that implements <see cref="IUnmanagedInterfaceType{TInterface}"/> to enable invoking methods in the virtual method table.
    /// </summary>
    /// <typeparam name="T">The type to use to represent the the identity of the unmanaged type.</typeparam>
    public unsafe interface IUnmanagedVirtualMethodTableProvider
    {
        /// <summary>
        /// Get the information about the virtual method table for a given unmanaged interface type represented by <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The managed type for the unmanaged interface.</param>
        /// <returns>The virtual method table information for the unmanaged interface.</returns>
        protected VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type);

        /// <summary>
        /// Get the information about the virtual method table for the given unmanaged interface type.
        /// </summary>
        /// <typeparam name="TUnmanagedInterfaceType">The managed interface type that represents the unmanaged interface.</typeparam>
        /// <returns>The virtual method table information for the unmanaged interface.</returns>
        public sealed VirtualMethodTableInfo GetVirtualMethodTableInfoForKey<TUnmanagedInterfaceType>()
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType>
        {
            return GetVirtualMethodTableInfoForKey(typeof(TUnmanagedInterfaceType));
        }

        /// <summary>
        /// Get the length of the virtual method table for the given unmanaged interface type.
        /// </summary>
        /// <typeparam name="TUnmanagedInterfaceType">The managed interface type that represents the unmanaged interface.</typeparam>
        /// <returns>The length of the virtual method table for the unmanaged interface.</returns>
        public static int GetVirtualMethodTableLength<TUnmanagedInterfaceType>()
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType>
        {
            return TUnmanagedInterfaceType.VirtualMethodTableLength;
        }

        /// <summary>
        /// Get a pointer to the virtual method table  of managed implementations of the unmanaged interface type.
        /// </summary>
        /// <typeparam name="TUnmanagedInterfaceType">The managed interface type that represents the unmanaged interface.</typeparam>
        /// <returns>A pointer to the virtual method table  of managed implementations of the unmanaged interface type</returns>
        public static void* GetVirtualMethodTableManagedImplementation<TUnmanagedInterfaceType>()
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType>
        {
            return TUnmanagedInterfaceType.VirtualMethodTableManagedImplementation;
        }

        /// <summary>
        /// Get a pointer that wraps a managed implementation of an unmanaged interface that can be passed to unmanaged code.
        /// </summary>
        /// <typeparam name="TUnmanagedInterfaceType">The managed type that represents the unmanaged interface.</typeparam>
        /// <param name="obj">The managed object that implements the unmanaged interface.</param>
        /// <returns>A pointer-sized value that can be passed to unmanaged code that represents <paramref name="obj"/></returns>
        public static void* GetUnmanagedWrapperForObject<TUnmanagedInterfaceType>(TUnmanagedInterfaceType obj)
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType>
        {
            return TUnmanagedInterfaceType.GetUnmanagedWrapperForObject(obj);
        }

        /// <summary>
        /// Get the object wrapped by <paramref name="ptr"/>.
        /// </summary>
        /// <typeparam name="TUnmanagedInterfaceType">The managed type that represents the unmanaged interface.</typeparam>
        /// <param name="ptr">A pointer-sized value returned by <see cref="GetUnmanagedWrapperForObject{TUnmanagedInterfaceType}(TUnmanagedInterfaceType)"/> or <see cref="IUnmanagedInterfaceType{TInterface, TKey}.GetUnmanagedWrapperForObject(TInterface)"/>.</param>
        /// <returns>The object wrapped by <paramref name="ptr"/>.</returns>
        public static TUnmanagedInterfaceType GetObjectForUnmanagedWrapper<TUnmanagedInterfaceType>(void* ptr)
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType>
        {
            return TUnmanagedInterfaceType.GetObjectForUnmanagedWrapper(ptr);
        }
    }

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
