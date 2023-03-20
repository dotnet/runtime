﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
#pragma warning disable SYSLIB1055 // The managed type 'System.Exception' for entry-point marshaller type 'System.Runtime.InteropServices.Marshalling.ExceptionHResultMarshaller<T>' must be a closed generic type, have the same arity as the managed type if it is a value marshaller, or have one additional generic parameter if it is a collection marshaller.

    [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(ExceptionHResultMarshaller<>))]
#pragma warning restore SYSLIB1055
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
