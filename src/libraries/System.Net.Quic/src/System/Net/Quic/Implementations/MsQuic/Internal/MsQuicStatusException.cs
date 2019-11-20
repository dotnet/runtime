using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            string errorCode = MsQuicConstants.ErrorTypeFromErrorCode(status);
            return $"Quic Error: {errorCode}. " + message;
        }

        internal static void ThrowIfFailed(uint status, string message = null, Exception innerException = null)
        {
            if (!MsQuicStatusHelper.Succeeded(status))
            {
                throw new MsQuicStatusException(status, message, innerException);
            }
        }
    }
}
