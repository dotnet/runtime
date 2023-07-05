// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Tests
{
    public unsafe class VTableGCHandlePair<TUnmanagedInterface> : IUnmanagedObjectUnwrapper
        where TUnmanagedInterface : IUnmanagedInterfaceType
    {
        public static void* Allocate(TUnmanagedInterface obj)
        {
            void** unmanaged = (void**)NativeMemory.Alloc((nuint)sizeof(void*) * 2);
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

        static object IUnmanagedObjectUnwrapper.GetObjectForUnmanagedWrapper(void* ptr) => 
            (TUnmanagedInterface)GCHandle.FromIntPtr((nint)((void**)ptr)[1]).Target;
    }
}
