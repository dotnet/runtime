// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices
{
    public readonly ref struct VirtualMethodTableInfo
    {
        public VirtualMethodTableInfo(IntPtr thisPointer, ReadOnlySpan<IntPtr> virtualMethodTable)
        {
            ThisPointer = thisPointer;
            VirtualMethodTable = virtualMethodTable;
        }

        public IntPtr ThisPointer { get; }
        public ReadOnlySpan<IntPtr> VirtualMethodTable { get; }

        public void Deconstruct(out IntPtr thisPointer, out ReadOnlySpan<IntPtr> virtualMethodTable)
        {
            thisPointer = ThisPointer;
            virtualMethodTable = VirtualMethodTable;
        }
    }

    public unsafe interface IUnmanagedVirtualMethodTableProvider<T> where T : IEquatable<T>
    {
        protected VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(T typeKey);

        public sealed VirtualMethodTableInfo GetVirtualMethodTableInfoForKey<TUnmanagedInterfaceType>()
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType, T>
        {
            return GetVirtualMethodTableInfoForKey(TUnmanagedInterfaceType.TypeKey);
        }

        public static void* GetVirtualMethodTableManagedImplementation<TUnmanagedInterfaceType>()
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType, T>
        {
            return TUnmanagedInterfaceType.VirtualMethodTableManagedImplementation;
        }

        public static void* GetUnmanagedWrapperForObject<TUnmanagedInterfaceType>(TUnmanagedInterfaceType obj)
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType, T>
        {
            return TUnmanagedInterfaceType.GetUnmanagedWrapperForObject(obj);
        }

        public static TUnmanagedInterfaceType GetObjectForUnmanagedWrapper<TUnmanagedInterfaceType>(void* ptr)
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<TUnmanagedInterfaceType, T>
        {
            return TUnmanagedInterfaceType.GetObjectForUnmanagedWrapper(ptr);
        }
    }

    public unsafe interface IUnmanagedInterfaceType<TInterface, TKey>
        where TInterface : IUnmanagedInterfaceType<TInterface, TKey>
        where TKey : IEquatable<TKey>
    {
        public static abstract void* VirtualMethodTableManagedImplementation { get; }

        public static abstract void* GetUnmanagedWrapperForObject(TInterface obj);

        public static abstract TInterface GetObjectForUnmanagedWrapper(void* ptr);

        public static abstract TKey TypeKey { get; }
    }
}
