using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct QuicMethods
    {
        private static QuicMethods instance = new QuicMethods
        {
            setEncryptionSecrets =
                Marshal.GetFunctionPointerForDelegate(new SetEncryptionSecretsFunc(SetEncryptionSecretsImpl)),
            addHandshakeData = Marshal.GetFunctionPointerForDelegate(new AddHandshakeDataFunc(AddHandshakeDataImpl)),
            flushFlight = Marshal.GetFunctionPointerForDelegate(new FlushFlightFunc(FlushFlightImpl)),
            sendAlert = Marshal.GetFunctionPointerForDelegate(new SendAlertFunc(SendAlertImpl))
        };

        public static ref QuicMethods Instance => ref instance;

        // these delegates need to match the ssl_quic_method_st struct in openssl/ssl.h
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetEncryptionSecretsFunc(Ssl ssl, SslEncryptionLevel level, byte* readSecret,
            byte* writeSecret, UIntPtr secretLen);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int AddHandshakeDataFunc(Ssl ssl, SslEncryptionLevel level, byte* data, UIntPtr len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FlushFlightFunc(Ssl ssl);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SendAlertFunc(Ssl ssl, SslEncryptionLevel level, byte alert);

        // do not reorder these
        private IntPtr setEncryptionSecrets;
        private IntPtr addHandshakeData;
        private IntPtr flushFlight;
        private IntPtr sendAlert;

        private static IQuicCallback GetCallbackInterface(Ssl ssl)
        {
            var addr = ssl.GetCallbackInterface();
            var callback = (IQuicCallback) GCHandle.FromIntPtr(addr).Target!;

            return callback;
        }

        public static int SetEncryptionSecretsImpl(Ssl ssl, SslEncryptionLevel level, byte* readSecret,
            byte* writeSecret, UIntPtr secretLen)
        {
            var callback = GetCallbackInterface(ssl);

            var readS = new Span<byte>(readSecret, (int) secretLen.ToUInt32());
            var writeS = new Span<byte>(writeSecret, (int) secretLen.ToUInt32());

            return callback.SetEncryptionSecrets(level, readS.ToArray(), writeS.ToArray());
        }

        public static int AddHandshakeDataImpl(Ssl ssl, SslEncryptionLevel level, byte* data, UIntPtr len)
        {
            var callback = GetCallbackInterface(ssl);

            var span = new Span<byte>(data, (int) len.ToUInt32());

            return callback.AddHandshakeData(level, span.ToArray());
        }

        public static int FlushFlightImpl(Ssl ssl)
        {
            var callback = GetCallbackInterface(ssl);

            return callback.Flush();
        }

        public static int SendAlertImpl(Ssl ssl, SslEncryptionLevel level, byte alert)
        {
            var callback = GetCallbackInterface(ssl);

            return callback.SendAlert(level, (TlsAlert) alert);
        }
    }
}
