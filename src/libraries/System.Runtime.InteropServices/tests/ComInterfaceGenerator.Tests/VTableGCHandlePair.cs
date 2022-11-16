// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ComInterfaceGenerator.Tests
{
    public static unsafe class VTableGCHandlePair<TUnmanagedInterface, TKey>
        where TUnmanagedInterface : IUnmanagedInterfaceType<TUnmanagedInterface, TKey>
        where TKey : IEquatable<TKey>
    {
        public static void* Allocate(TUnmanagedInterface obj)
        {
            void** unmanaged = (void**)NativeMemory.Alloc((nuint)sizeof(void*) * (nuint)IUnmanagedVirtualMethodTableProvider<TKey>.GetVirtualMethodTableLength<TUnmanagedInterface>());
            unmanaged[0] = TUnmanagedInterface.VirtualMethodTableManagedImplementation;
            unmanaged[1] = (void*)GCHandle.ToIntPtr(GCHandle.Alloc(obj));
            return unmanaged;
        }

        public static void Free(void* pair)
        {
            GCHandle.FromIntPtr((nint)((void**)pair)[1]).Free();
            NativeMemory.Free(pair);
        }

        public static TUnmanagedInterface GetObject(void* pair)
        {
            return (TUnmanagedInterface)GCHandle.FromIntPtr((nint)((void**)pair)[1]).Target;
        }
    }
}
