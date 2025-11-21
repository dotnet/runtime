// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NetworkFramework
    {
        // Network Framework reference counting functions
        [LibraryImport(Libraries.NetworkFramework, EntryPoint = "nw_retain")]
        internal static partial IntPtr Retain(IntPtr obj);

        [LibraryImport(Libraries.NetworkFramework, EntryPoint = "nw_release")]
        internal static partial void Release(IntPtr obj);

        // Network Framework error domains
        internal enum NetworkFrameworkErrorDomain
        {
            Invalid = 0,
            POSIX = 1,
            DNS = 2,
            TLS = 3
        }

        internal enum NWErrorDomainPOSIX
        {
            OperationCanceled = 89, // ECANCELED
        }

        internal sealed class NetworkFrameworkException : Exception
        {
            public int ErrorCode { get; }
            public NetworkFrameworkErrorDomain ErrorDomain { get; }

            internal NetworkFrameworkException()
            {
            }

            internal NetworkFrameworkException(int errorCode, NetworkFrameworkErrorDomain errorDomain, string? message)
                : base(message ?? $"Network Framework error {errorCode} in domain {errorDomain}")
            {
                HResult = errorCode;
                ErrorCode = errorCode;
                ErrorDomain = errorDomain;
            }

            internal NetworkFrameworkException(int errorCode, NetworkFrameworkErrorDomain errorDomain, string? message, Exception innerException)
                : base(message ?? $"Network Framework error {errorCode} in domain {errorDomain}", innerException)
            {
                HResult = errorCode;
                ErrorCode = errorCode;
                ErrorDomain = errorDomain;
            }

            public override string ToString()
            {
                return $"{base.ToString()}, ErrorCode: {ErrorCode}, ErrorDomain: {ErrorDomain}";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct NetworkFrameworkError
        {
            public int ErrorCode;
            public int ErrorDomain;
            public byte* ErrorMessage; // C string of NULL
        }

        internal static unsafe Exception CreateExceptionForNetworkFrameworkError(in NetworkFrameworkError error)
        {
            string? message = null;
            NetworkFrameworkErrorDomain domain = (NetworkFrameworkErrorDomain)error.ErrorDomain;

            if (error.ErrorMessage != null)
            {
                message = Utf8StringMarshaller.ConvertToManaged(error.ErrorMessage);
            }

            return new NetworkFrameworkException(error.ErrorCode, domain, message);
        }
    }
}
