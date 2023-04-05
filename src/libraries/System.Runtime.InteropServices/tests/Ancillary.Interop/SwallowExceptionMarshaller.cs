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
    /// Marshaller that swallows the exception.
    /// </summary>
    [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionHResultMarshaller<>))]
    public static class SwallowExceptionMarshaller
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
