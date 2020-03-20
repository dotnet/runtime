using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Ssl
    {
        private static readonly int managedInterfaceIndex =
            GetExNewIndex(Interop.OpenSsl.CRYPTO_EX_INDEX_SSL, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        public static Ssl Null => default(Ssl);

        private readonly IntPtr handle;

        public string Version =>
            Marshal.PtrToStringUTF8(new IntPtr(Interop.OpenSsl.SSL_get_version(handle)))!;

        public TlsVersion MinProtoVersion
        {
            get => (TlsVersion) Ctrl(SslCtrlCommand.GetMinProtoVersion, 0, IntPtr.Zero);
            set => Ctrl(SslCtrlCommand.SetMinProtoVersion, (long) value, IntPtr.Zero);
        }

        public TlsVersion MaxProtoVersion
        {
            get => (TlsVersion) Ctrl(SslCtrlCommand.GetMaxProtoVersion, 0, IntPtr.Zero);
            set => Ctrl(SslCtrlCommand.SetMaxProtoVersion, (long) value, IntPtr.Zero);
        }

        private Ssl(SslContext ctx)
        {
            handle = Interop.OpenSsl.SSL_new(ctx);
        }

        public static Ssl New(SslContext ctx)
        {
            return new Ssl(ctx);
        }

        public static void Free(Ssl ssl)
        {
            Interop.OpenSsl.SSL_free(ssl.handle);
        }

        public int UseCertificateFile(string file, SslFiletype type)
        {
            return Interop.OpenSsl.SSL_use_certificate_file(handle, file, type);
        }

        public int UsePrivateKeyFile(string file, SslFiletype type)
        {
            return Interop.OpenSsl.SSL_use_PrivateKey_file(handle, file, type);
        }

        public int SetQuicMethod(ref QuicMethods methods)
        {
            return Interop.OpenSsl.SSL_set_quic_method(handle, ref methods);
        }

        public int SetAcceptState()
        {
            return Interop.OpenSsl.SSL_set_accept_state(handle);
        }

        public int SetConnectState()
        {
            return Interop.OpenSsl.SSL_set_connect_state(handle);
        }

        public int DoHandshake()
        {
            return Interop.OpenSsl.SSL_do_handshake(handle);
        }

        public int Ctrl(SslCtrlCommand cmd, long larg, IntPtr parg)
        {
            return Interop.OpenSsl.SSL_ctrl(handle, cmd, larg, parg);
        }

        public SslError GetError(int code)
        {
            return (SslError) Interop.OpenSsl.SSL_get_error(handle, code);
        }

        public int ProvideQuicData(SslEncryptionLevel level, ReadOnlySpan<byte> data)
        {
            fixed (byte* pData = data)
            {
                return Interop.OpenSsl.SSL_provide_quic_data(handle, level, pData, new IntPtr(data.Length));
            }
        }

        public int SetExData(int idx, IntPtr data)
        {
            return Interop.OpenSsl.SSL_set_ex_data(handle, idx, data);
        }

        public IntPtr GetExData(int idx)
        {
            return Interop.OpenSsl.SSL_get_ex_data(handle, idx);
        }

        public void SetCallbackInterface(IntPtr address)
        {
            SetExData(managedInterfaceIndex, address);
        }

        public IntPtr GetCallbackInterface()
        {
            return GetExData(managedInterfaceIndex);
        }

        public int SetTlsexHostName(string hostname)
        {
            var addr = Marshal.StringToHGlobalAnsi(hostname);
            var res = Ctrl(SslCtrlCommand.SetTlsextHostname, 0, addr);
            Marshal.FreeHGlobal(addr);
            return res;
        }

        public static int GetExNewIndex(long argl, IntPtr argp, IntPtr newFunc, IntPtr dupFunc,
            IntPtr freeFunc)
        {
            return Interop.OpenSsl.CRYPTO_get_ex_new_index(Interop.OpenSsl.CRYPTO_EX_INDEX_SSL, argl, argp, newFunc, dupFunc, freeFunc);
        }

        public override string ToString()
        {
            return handle.ToString();
        }

    }
}
