// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Types that are only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
// These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).

namespace System.Runtime.InteropServices.Marshalling
{
    // This type implements the logic to get the managed object from the unmanaged "this" pointer.
    // If we decide to not expose the VTable source generator, we don't need to expose this and we can just inline the logic
    // into the generated code in the source generator.
    public sealed unsafe class ComWrappersUnwrapper : IUnmanagedObjectUnwrapper
    {
        public static object GetObjectForUnmanagedWrapper(void* ptr)
        {
            return ComWrappers.ComInterfaceDispatch.GetInstance<object>((ComWrappers.ComInterfaceDispatch*)ptr);
        }
    }
}
