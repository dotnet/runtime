// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
// These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).

namespace System.Runtime.InteropServices.Marshalling;

// This type is purely conceptual for the purposes of the Lowered.Example.cs code. We will likely never ship this.
// The analyzer currently doesn't support marshallers where the managed type is 'void'.
#pragma warning disable SYSLIB1057 // The type 'System.Runtime.InteropServices.Marshalling.PreserveSigMarshaller' specifies it supports the 'ManagedToUnmanagedOut' marshal mode, but it does not provide a 'ConvertToManaged' method that takes the unmanaged type as a parameter and returns 'void'.
[CustomMarshaller(typeof(void), MarshalMode.ManagedToUnmanagedOut, typeof(PreserveSigMarshaller))]
#pragma warning restore SYSLIB1057
internal static class PreserveSigMarshaller
{
    public static void ConvertToManaged(int hr)
    {
        Marshal.ThrowExceptionForHR(hr);
    }
}
