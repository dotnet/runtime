// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
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

        internal sealed class NetworkFrameworkException : Exception
        {
            public int ErrorCode { get; }
            public int ErrorDomain { get; }

            internal NetworkFrameworkException()
            {
            }

            internal NetworkFrameworkException(int errorCode, int errorDomain, string? message)
                : base(message ?? $"Network Framework error {errorCode} in domain {GetDomainName(errorDomain)}")
            {
                HResult = errorCode;
                ErrorCode = errorCode;
                ErrorDomain = errorDomain;
            }

            internal NetworkFrameworkException(int errorCode, int errorDomain, string? message, Exception innerException)
                : base(message ?? $"Network Framework error {errorCode} in domain {GetDomainName(errorDomain)}", innerException)
            {
                HResult = errorCode;
                ErrorCode = errorCode;
                ErrorDomain = errorDomain;
            }

            private static string GetDomainName(int domain)
            {
                return domain switch
                {
                    0 => "Invalid",
                    1 => "POSIX",
                    2 => "DNS",
                    3 => "TLS",
                    _ => $"Unknown({domain})"
                };
            }

            public override string ToString()
            {
                return $"{base.ToString()}, ErrorCode: {ErrorCode}, ErrorDomain: {ErrorDomain}";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NetworkFrameworkError
        {
            public int ErrorCode;
            public int ErrorDomain;
            public IntPtr ErrorMessage; // CFStringRef for most errors, C string for POSIX errors
        }

        internal static Exception CreateExceptionForNetworkFrameworkError(in NetworkFrameworkError error)
        {
            string? message = null;
            if (error.ErrorMessage != IntPtr.Zero)
            {
                // For POSIX errors (domain 1), the message is a regular C string from strerror()
                // For other errors, it's a CFString
                if (error.ErrorDomain == 1) // kNWErrorDomainPOSIX
                {
                    message = Marshal.PtrToStringUTF8(error.ErrorMessage);
                }
                else
                {
                    using (var cfString = new SafeCFStringHandle(error.ErrorMessage, ownsHandle: false))
                    {
                        message = CoreFoundation.CFStringToString(cfString);
                    }
                }
            }

            return new NetworkFrameworkException(error.ErrorCode, error.ErrorDomain, message);
        }
    }
}
