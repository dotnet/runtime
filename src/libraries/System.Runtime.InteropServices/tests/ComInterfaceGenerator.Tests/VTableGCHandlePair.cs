// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace ComInterfaceGenerator.Tests
{
    public static unsafe class VTableGCHandlePair<TUnmanagedInterface>
        where TUnmanagedInterface : IUnmanagedInterfaceType<TUnmanagedInterface>
    {
        public static void* Allocate(TUnmanagedInterface obj)
        {
            void** unmanaged = (void**)NativeMemory.Alloc((nuint)sizeof(void*) * (nuint)IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableLength<TUnmanagedInterface>());
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
