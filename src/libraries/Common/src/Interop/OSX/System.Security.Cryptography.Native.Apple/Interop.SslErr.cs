// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal sealed class SslException : Exception
        {
            internal SslException()
            {
            }

            internal SslException(int errorCode, string? message)
                : base(message)
            {
                HResult = errorCode;
            }
        }
    }

    internal static partial class AppleCrypto
    {
        internal static Exception CreateExceptionForOSStatus(int osStatus)
        {
            string? msg = GetSecErrorString(osStatus);

            // msg might be null, but that's OK
            return new SslException(osStatus, msg);
        }
    }
}
