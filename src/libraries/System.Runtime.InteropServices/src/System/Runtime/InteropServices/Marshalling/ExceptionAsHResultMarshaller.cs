// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

// This type is only needed for the VTable source generator or to provide abstract concepts that the COM generator would use under the hood.
// These are types that we can exclude from the API proposals and either inline into the generated code, provide as file-scoped types, or not provide publicly (indicated by comments on each type).

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshals an exception object to the value of its <see cref="Exception.HResult"/> converted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to convert the HResult to.</typeparam>
    /// <remarks>
    /// This type is used by the COM source generator to enable marshalling exceptions to the HResult of the exception.
    /// We can skip the exposing the exception marshallers if we decide to not expose the VTable source generator.
    /// In that case, we'd hard-code the implementations of these marshallers into the COM source generator.
    /// </remarks>
    [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionAsHResultMarshaller<>))]
    public static class ExceptionAsHResultMarshaller<T>
        where T : unmanaged, INumber<T>
    {
        /// <summary>
        /// Marshals an exception object to the value of its <see cref="Exception.HResult"/> converted to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="e">The exception.</param>
        /// <returns>The HResult of the exception, converted to <typeparamref name="T"/>.</returns>
        public static T ConvertToUnmanaged(Exception e)
        {
            // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
            // and calls SetErrorInfo if the platform supports it.

            // We use CreateTruncating here to convert from the int return type of Marshal.GetHRForException
            // to whatever the T is. A "truncating" conversion in this case is the same as an unchecked conversion like
            // (uint)Marshal.GetHRForException(e) would be if we were writing a non-generic marshaller.
            // Since we're using the INumber<T> interface, this is the correct mechanism to represent that conversion.
            return T.CreateTruncating(Marshal.GetHRForException(e));
        }
    }
}
