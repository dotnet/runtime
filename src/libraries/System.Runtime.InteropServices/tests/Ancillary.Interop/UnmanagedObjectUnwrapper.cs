// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling;

// This type should be inlined in the COM source generator and made internal
// This type allows the generated code to call a private explicit implementation of IUnmanagedObjectUnwrapper.GetObjectForUnmanagedWrapper
public unsafe static class UnmanagedObjectUnwrapper
{
    public static object GetObjectForUnmanagedWrapper<T>(void* ptr) where T : IUnmanagedObjectUnwrapper
    {
        return T.GetObjectForUnmanagedWrapper(ptr);
    }

    // This type is provided for the unit tests that only confirm the generated code compiles
    public class TestUnwrapper : IUnmanagedObjectUnwrapper
    {
        public static object GetObjectForUnmanagedWrapper(void* ptr) => throw new NotImplementedException();
    }
}
