using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Ssl
    {
        private static readonly int managedInterfaceIndex =
            GetExNewIndex(Interop.OpenSslQuic.CRYPTO_EX_INDEX_SSL, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        public static Ssl Null => default(Ssl);

        private readonly IntPtr handle;

        public string Version =>
            Marshal.PtrToStringUTF8(new IntPtr(Interop.OpenSslQuic.SslGetVersion(handle)))!;

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
            handle = Interop.OpenSslQuic.SslNew(ctx);
        }

        public static Ssl New(SslContext ctx)
        {
            return new Ssl(ctx);
        }

        public static void Free(Ssl ssl)
        {
            Interop.OpenSslQuic.SslFree(ssl.handle);
        }

        public int UseCertificateFile(string file, SslFiletype type)
        {
            return Interop.OpenSslQuic.SslUseCertificateFile(handle, file, type);
        }

        public int UsePrivateKeyFile(string file, SslFiletype type)
        {
            return Interop.OpenSslQuic.SslUsePrivateKeyFile(handle, file, type);
        }

        public int SetQuicMethod(ref QuicMethods methods)
        {
            return Interop.OpenSslQuic.SslSetQuicMethod(handle, ref methods);
        }

        public int SetAcceptState()
        {
            return Interop.OpenSslQuic.SslSetAcceptState(handle);
        }

        public int SetConnectState()
        {
            return Interop.OpenSslQuic.SslSetConnectState(handle);
        }

        public int DoHandshake()
        {
            return Interop.OpenSslQuic.SslDoHandshake(handle);
        }

        public int Ctrl(SslCtrlCommand cmd, long larg, IntPtr parg)
        {
            return Interop.OpenSslQuic.SslCtrl(handle, cmd, larg, parg);
        }

        public SslError GetError(int code)
        {
            return (SslError) Interop.OpenSslQuic.SslGetError(handle, code);
        }

        public int ProvideQuicData(SslEncryptionLevel level, ReadOnlySpan<byte> data)
        {
            fixed (byte* pData = data)
            {
                return Interop.OpenSslQuic.SslProvideQuicData(handle, level, pData, new IntPtr(data.Length));
            }
        }

        public int SetExData(int idx, IntPtr data)
        {
            return Interop.OpenSslQuic.SslSetExData(handle, idx, data);
        }

        public IntPtr GetExData(int idx)
        {
            return Interop.OpenSslQuic.SslGetExData(handle, idx);
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
            return Interop.OpenSslQuic.CryptoGetExNewIndex(Interop.OpenSslQuic.CRYPTO_EX_INDEX_SSL, argl, argp, newFunc, dupFunc, freeFunc);
        }

        public override string ToString()
        {
            return handle.ToString();
        }

    }
}
