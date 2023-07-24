// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
// These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).

namespace System.Runtime.InteropServices.Marshalling
{
    // This attribute provides the mechanism for the VTable source generator to know which type to use to get the managed object
    // from the unmanaged "this" pointer. If we decide to not expose VirtualMethodIndexAttribute, we don't need to expose this.
    [AttributeUsage(AttributeTargets.Interface)]
    public class UnmanagedObjectUnwrapperAttribute<TMapper> : Attribute
        where TMapper : IUnmanagedObjectUnwrapper
    {
    }
}
