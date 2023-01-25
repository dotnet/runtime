// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM Source Generator and defines basic vtable interactions that we would need in the COM source generator in one form or another.
namespace System.Runtime.InteropServices.Marshalling;

/// <summary>
/// This interface allows an object to provide information about a virtual method table for a managed interface to enable invoking methods in the virtual method table.
/// </summary>
public unsafe interface IUnmanagedVirtualMethodTableProvider
{
    /// <summary>
    /// Get the information about the virtual method table for a given unmanaged interface type represented by <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The managed type for the unmanaged interface.</param>
    /// <returns>The virtual method table information for the unmanaged interface.</returns>
    public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type);

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
