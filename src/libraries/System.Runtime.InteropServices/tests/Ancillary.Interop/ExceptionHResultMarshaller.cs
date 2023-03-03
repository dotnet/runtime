// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshals an exception object to the value of its <see cref="Exception.HResult"/> converted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to convert the HResult to.</typeparam>
    [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionDefaultMarshaller<>))]
    public static class ExceptionHResultMarshaller<T>
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
