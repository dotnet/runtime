// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Converts the exception to the default value of the unmanaged type.
    /// </summary>
    /// <typeparam name="T">The unmanaged type</typeparam>
    [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionAsDefaultMarshaller<>))]
    public static class ExceptionAsDefaultMarshaller<T>
        where T : unmanaged
    {
        /// <summary>
        /// Converts the exception to the default value of the unmanaged type.
        /// </summary>
        /// <param name="e">The exception</param>
        /// <returns>The default value of <typeparamref name="T"/>.</returns>
        public static T ConvertToUnmanaged(Exception e)
        {
            // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
            // and calls SetErrorInfo if the platform supports it.
            _ = Marshal.GetHRForException(e);
            return default;
        }
    }
}
