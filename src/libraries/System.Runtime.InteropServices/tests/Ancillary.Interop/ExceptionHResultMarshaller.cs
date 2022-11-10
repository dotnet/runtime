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
        public static T ConvertToUnmanaged(Exception e)
        {
            // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
            // and calls SetErrorInfo if the platform suppots it.
            // TODO: Should we use the built-in COM interop support for this, or should we use the generator to implement
            // this experience?
            return T.CreateTruncating(Marshal.GetHRForException(e));
        }
    }
}
