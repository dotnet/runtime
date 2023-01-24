// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Types that are only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
// These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).

using System.Numerics;

namespace System.Runtime.InteropServices.Marshalling;

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

// This type is purely conceptual for the purposes of the Lowered.Example.cs code. We will likely never ship this.
// The analyzer currently doesn't support marshallers where the managed type is 'void'.
#pragma warning disable SYSLIB1057 // The type 'System.Runtime.InteropServices.Marshalling.PreserveSigMarshaller' specifies it supports the 'ManagedToUnmanagedOut' marshal mode, but it does not provide a 'ConvertToManaged' method that takes the unmanaged type as a parameter and returns 'void'.
[CustomMarshaller(typeof(void), MarshalMode.ManagedToUnmanagedOut, typeof(PreserveSigMarshaller))]
#pragma warning restore SYSLIB1057
public static class PreserveSigMarshaller
{
    public static void ConvertToManaged(int hr)
    {
        Marshal.ThrowExceptionForHR(hr);
    }
}


// This attribute provides the mechanism for the VTable source generator to know which type to use to get the managed object
// from the unmanaged "this" pointer. If we decide to not expose VirtualMethodIndexAttribute, we don't need to expose this.
[AttributeUsage(AttributeTargets.Interface)]
public class UnmanagedObjectUnwrapperAttribute<TMapper> : Attribute
    where TMapper : IUnmanagedObjectUnwrapper
{
}

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
