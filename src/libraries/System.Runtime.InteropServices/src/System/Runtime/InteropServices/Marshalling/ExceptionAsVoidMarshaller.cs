// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller that swallows the exception.
    /// </summary>
#pragma warning disable SYSLIB1057 // Marshaller type does not have the required shape
                                   // This marshaller has 'void' as the unmanaged type,
                                   // which is technically invalid but is the correct type for this case.
                                   // This scenario is specially handled for exception marshalling to make this work.
    [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionAsVoidMarshaller))]
#pragma warning restore SYSLIB1057 // Marshaller type does not have the required shape
    public static class ExceptionAsVoidMarshaller
    {
        /// <summary>
        /// Swallow the exception and return nothing.
        /// </summary>
        /// <param name="e">The exception.</param>
        public static void ConvertToUnmanaged(Exception e)
        {
            // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
            // and calls SetErrorInfo if the platform supports it.
            _ = Marshal.GetHRForException(e);
        }
    }
}
