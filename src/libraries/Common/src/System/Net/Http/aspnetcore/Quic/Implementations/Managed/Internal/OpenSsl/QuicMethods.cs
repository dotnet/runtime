using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    internal static class QuicMethods
    {
        private static readonly int _managedInterfaceIndex =
            Interop.OpenSslQuic.CryptoGetExNewIndex(Interop.OpenSslQuic.CRYPTO_EX_INDEX_SSL, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        private static unsafe OpenSslQuicMethods.NativeCallbacks _callbacks = new OpenSslQuicMethods.NativeCallbacks()
        {
            setEncryptionSecrets =
                Marshal.GetFunctionPointerForDelegate(new OpenSslQuicMethods.SetEncryptionSecretsFunc(SetEncryptionSecretsImpl)),
            addHandshakeData = Marshal.GetFunctionPointerForDelegate(new OpenSslQuicMethods.AddHandshakeDataFunc(AddHandshakeDataImpl)),
            flushFlight = Marshal.GetFunctionPointerForDelegate(new OpenSslQuicMethods.FlushFlightFunc(FlushFlightImpl)),
            sendAlert = Marshal.GetFunctionPointerForDelegate(new OpenSslQuicMethods.SendAlertFunc(SendAlertImpl))
        };

        private static ref OpenSslQuicMethods.NativeCallbacks Callbacks => ref _callbacks;

        internal static void InitCallbacks(GCHandle handle, IntPtr ssl)
        {
            Interop.OpenSslQuic.SslSetQuicMethod(ssl, ref Callbacks);

            // add the callback as contextual data so we can retrieve it inside the callback
            Interop.OpenSslQuic.SslSetExData(ssl, _managedInterfaceIndex, GCHandle.ToIntPtr(handle));
        }

        internal static unsafe void DeinitCallbacks(IntPtr ssl)
        {
            // call SslSetQuicMethod(ssl, null) to stop callbacks being called
            Interop.OpenSslQuic.SslSetQuicMethod(ssl, ref Unsafe.AsRef<OpenSslQuicMethods.NativeCallbacks>(null));
        }

        private static ManagedQuicConnection GetCallbackInterface(IntPtr ssl)
        {
            var addr = Interop.OpenSslQuic.SslGetExData(ssl, _managedInterfaceIndex);
            var callback = (ManagedQuicConnection) GCHandle.FromIntPtr(addr).Target!;

            return callback;
        }

        private static unsafe int SetEncryptionSecretsImpl(IntPtr ssl, SslEncryptionLevel level, byte* readSecret,
            byte* writeSecret, UIntPtr secretLen)
        {
            var callback = GetCallbackInterface(ssl);

            var readS = new ReadOnlySpan<byte>(readSecret, (int) secretLen.ToUInt32());
            var writeS = new ReadOnlySpan<byte>(writeSecret, (int) secretLen.ToUInt32());

            return callback.HandleSetEncryptionSecrets(level, readS, writeS);
        }

        private static unsafe int AddHandshakeDataImpl(IntPtr ssl, SslEncryptionLevel level, byte* data, UIntPtr len)
        {
            var callback = GetCallbackInterface(ssl);

            var span = new ReadOnlySpan<byte>(data, (int) len.ToUInt32());

            return callback.HandleAddHandshakeData(level, span);
        }

        private static int FlushFlightImpl(IntPtr ssl)
        {
            var callback = GetCallbackInterface(ssl);

            return callback.HandleFlush();
        }

        private static int SendAlertImpl(IntPtr ssl, SslEncryptionLevel level, byte alert)
        {
            var callback = GetCallbackInterface(ssl);

            return callback.HandleSendAlert(level, (TlsAlert) alert);
        }
    }
}
