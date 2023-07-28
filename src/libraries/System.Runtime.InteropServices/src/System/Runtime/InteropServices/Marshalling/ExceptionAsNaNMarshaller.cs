// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Converts all exceptions to <see cref="IFloatingPointIeee754{T}.NaN"/>.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to return the <c>NaN</c> value for.</typeparam>
    [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionAsNaNMarshaller<>))]
    public static class ExceptionAsNaNMarshaller<T>
        where T : unmanaged, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Convert the exception to <see cref="IFloatingPointIeee754{T}.NaN"/>.
        /// </summary>
        /// <param name="e">The exception</param>
        /// <returns><see cref="IFloatingPointIeee754{T}.NaN"/>.</returns>
        public static T ConvertToUnmanaged(Exception e)
        {
            // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
            // and calls SetErrorInfo if the platform supports it.
            _ = Marshal.GetHRForException(e);
            return T.NaN;
        }
    }
}
