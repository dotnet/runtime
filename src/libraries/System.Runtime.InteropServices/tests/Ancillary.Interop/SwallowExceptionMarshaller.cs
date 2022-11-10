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
        public static void ConvertToUnmanaged(Exception e)
        {
            // Use GetHRForException to ensure the runtime sets up the IErrorInfo object
            // and calls SetErrorInfo if the platform suppots it.
            // TODO: Should we use the built-in COM interop support for this, or should we use the generator to implement
            // this experience?
            _ = Marshal.GetHRForException(e);
        }
    }
}
