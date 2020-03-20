using System;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    // TODO-RZ: remove this and use System.Security.Cryptography.Native
    internal static unsafe partial class OpenSsl
    {
        [DllImport(Interop.Libraries.Crypto)]
        internal static extern int CRYPTO_get_ex_new_index(int classIndex, long argl, IntPtr argp, IntPtr newFunc,
            IntPtr dupFunc, IntPtr freeFunc);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ErrorPrintCallback(byte* str, UIntPtr len, IntPtr u);

        [DllImport(Libraries.Crypto)]
        internal static extern int ERR_print_errors_cb(ErrorPrintCallback callback, IntPtr u);

        [DllImport(Libraries.Ssl)]
        internal static extern IntPtr SSL_CTX_new(SslMethod method);

        [DllImport(Libraries.Ssl)]
        internal static extern void SSL_CTX_free(IntPtr ctx);

        [DllImport(Libraries.Ssl)]
        internal static extern IntPtr SSL_CTX_set_client_cert_cb(SslMethod method);

        internal const int CRYPTO_EX_INDEX_SSL = 0;

        [DllImport(Libraries.Ssl)]
        internal static extern IntPtr SSL_new(SslContext ctx);

        [DllImport(Libraries.Ssl)]
        internal static extern void SSL_free(IntPtr ssl);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_use_certificate_file(IntPtr ssl, [MarshalAs(UnmanagedType.LPStr
            )]
            string file, SslFiletype type);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_use_PrivateKey_file(IntPtr ssl, [MarshalAs(UnmanagedType.LPStr
            )]
            string file, SslFiletype type);

        [DllImport(Libraries.Ssl)]
        internal static extern byte* SSL_get_version(IntPtr ssl);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_set_quic_method(IntPtr ssl, ref QuicMethods methods);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_set_accept_state(IntPtr ssl);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_set_connect_state(IntPtr ssl);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_do_handshake(IntPtr ssl);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_ctrl(IntPtr ssl, SslCtrlCommand cmd, long larg, IntPtr parg);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_get_error(IntPtr ssl, int code);

        [DllImport(Libraries.Ssl)]
        internal static extern int
            SSL_provide_quic_data(IntPtr ssl, SslEncryptionLevel level, byte* data, IntPtr len);

        [DllImport(Libraries.Ssl)]
        internal static extern int SSL_set_ex_data(IntPtr ssl, int idx, IntPtr data);

        [DllImport(Libraries.Ssl)]
        internal static extern IntPtr SSL_get_ex_data(IntPtr ssl, int idx);

        static OpenSsl()
        {
            ERR_print_errors_cb(PrintErrors, IntPtr.Zero);
        }

        private static int PrintErrors(byte* str, UIntPtr len, IntPtr _)
        {
            var span = new Span<byte>(str, (int) len.ToUInt32());
            Console.WriteLine(System.Text.Encoding.ASCII.GetString(span));
            return 1;
        }
    }
}
