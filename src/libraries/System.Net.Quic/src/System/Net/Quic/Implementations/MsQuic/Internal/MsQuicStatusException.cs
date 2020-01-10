// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal class MsQuicStatusException : Exception
    {
        internal MsQuicStatusException(uint status)
            : this(status, null)
        {
        }

        internal MsQuicStatusException(uint status, string message)
            : this(status, message, null)
        {
        }

        internal MsQuicStatusException(uint status, string message, Exception innerException)
            : base(GetMessage(status, message), innerException)
        {
            Status = status;
        }

        internal uint Status { get; }

        private static string GetMessage(uint status, string message)
        {
            string errorCode = MsQuicStatusCodes.GetError(status);
            return $"Quic Error: {errorCode}. " + message;
        }

        internal static void ThrowIfFailed(uint status, string message = null, Exception innerException = null)
        {
            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                throw new MsQuicStatusException(status, message, innerException);
            }
        }
    }
}
