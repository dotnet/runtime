// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM Source Generator and defines basic vtable interactions that we would need in the COM source generator in one form or another.
namespace System.Runtime.InteropServices.Marshalling;

// This type should be inlined in the COM source generator
public unsafe static class UnmanagedObjectUnwrapper
{
    public static object GetObjectForUnmanagedWrapper<T>(void* ptr) where T : IUnmanagedObjectUnwrapper
    {
        return T.GetObjectForUnmanagedWrapper(ptr);
    }
    public class DummyUnwrapper : IUnmanagedObjectUnwrapper
    {
        public static object GetObjectForUnmanagedWrapper(void* ptr) => throw new NotImplementedException();
    }
}
