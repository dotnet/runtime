using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class OpenSslQuicMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeCallbacks
        {
            internal IntPtr setEncryptionSecrets;
            internal IntPtr addHandshakeData;
            internal IntPtr flushFlight;
            internal IntPtr sendAlert;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int SetEncryptionSecretsFunc(IntPtr ssl, SslEncryptionLevel level, byte* readSecret,
            byte* writeSecret, UIntPtr secretLen);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int AddHandshakeDataFunc(IntPtr ssl, SslEncryptionLevel level, byte* data, UIntPtr len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int FlushFlightFunc(IntPtr ssl);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int SendAlertFunc(IntPtr ssl, SslEncryptionLevel level, byte alert);
    }
}
