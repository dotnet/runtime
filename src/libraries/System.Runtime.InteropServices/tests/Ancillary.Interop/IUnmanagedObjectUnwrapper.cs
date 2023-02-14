// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
// These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).

using System.Numerics;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// A factory to create an unmanaged "this pointer" from a managed object and to get a managed object from an unmanaged "this pointer".
    /// </summary>
    /// <remarks>
    /// This interface would be used by the VTable source generator to enable users to indicate how to get the managed object from the "this pointer".
    /// We can hard-code the ComWrappers logic here if we don't want to ship this interface.
    /// </remarks>
    public unsafe interface IUnmanagedObjectUnwrapper
    {
        /// <summary>
        /// Get the object wrapped by <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr">A an unmanaged "this pointer".</param>
        /// <returns>The object wrapped by <paramref name="ptr"/>.</returns>
        public static abstract object GetObjectForUnmanagedWrapper(void* ptr);
    }
}
