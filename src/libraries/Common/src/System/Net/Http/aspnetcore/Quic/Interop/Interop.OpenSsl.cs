using System;
using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;
using System.Net.Security;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    // TODO-RZ: remove this and use System.Security.Cryptography.Native
    internal static unsafe class OpenSslQuic
    {
        private static IntPtr __globalSslCtx = SslCtxNew(SslMethod.Tls);
        internal static IntPtr SslCreate()
        {
            return SslNew(__globalSslCtx);
        }

        [DllImport(Libraries.Crypto, EntryPoint = "CRYPTO_get_ex_new_index")]
        internal static extern int CryptoGetExNewIndex(int classIndex, long argl, IntPtr argp, IntPtr newFunc,
            IntPtr dupFunc, IntPtr freeFunc);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ErrorPrintCallback(byte* str, UIntPtr len, IntPtr u);

        [DllImport(Libraries.Crypto, EntryPoint = "ERR_print_errors_cb")]
        internal static extern int ErrPrintErrorsCb(ErrorPrintCallback callback, IntPtr u);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_CTX_new")]
        internal static extern IntPtr SslCtxNew(SslMethod method);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_CTX_free")]
        internal static extern void SslCtxFree(IntPtr ctx);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_CTX_set_client_cert_cb")]
        internal static extern IntPtr SslCtxSetClientCertCb(SslMethod method);

        internal const int CRYPTO_EX_INDEX_SSL = 0;

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_new")]
        internal static extern IntPtr SslNew(IntPtr ctx);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_free")]
        internal static extern void SslFree(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_use_certificate_file")]
        internal static extern int SslUseCertificateFile(IntPtr ssl, [MarshalAs(UnmanagedType.LPStr
            )]
            string file, SslFiletype type);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_use_PrivateKey_file")]
        internal static extern int SslUsePrivateKeyFile(IntPtr ssl, [MarshalAs(UnmanagedType.LPStr
            )]
            string file, SslFiletype type);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_get_version")]
        internal static extern byte* SslGetVersion(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_quic_method")]
        internal static extern int SslSetQuicMethod(IntPtr ssl, IntPtr methods);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_accept_state")]
        internal static extern int SslSetAcceptState(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_connect_state")]
        internal static extern int SslSetConnectState(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_do_handshake")]
        internal static extern int SslDoHandshake(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_ctrl")]
        internal static extern int SslCtrl(IntPtr ssl, SslCtrlCommand cmd, long larg, IntPtr parg);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_get_error")]
        internal static extern int SslGetError(IntPtr ssl, int code);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_provide_quic_data")]
        internal static extern int SslProvideQuicData(IntPtr ssl, OpenSslEncryptionLevel level, byte* data, IntPtr len);

        internal static int SslProvideQuicData(IntPtr ssl, OpenSslEncryptionLevel level, ReadOnlySpan<byte> data)
        {
            fixed (byte* pData = data)
            {
                return SslProvideQuicData(ssl, level, pData, new IntPtr(data.Length));
            }
        }

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_ex_data")]
        internal static extern int SslSetExData(IntPtr ssl, int idx, IntPtr data);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_get_ex_data")]
        internal static extern IntPtr SslGetExData(IntPtr ssl, int idx);

        internal static int SslSetTlsExHostName(IntPtr ssl, string hostname)
        {
            var addr = Marshal.StringToHGlobalAnsi(hostname);
            int res = SslCtrl(ssl, SslCtrlCommand.SetTlsextHostname, 0, addr);
            Marshal.FreeHGlobal(addr);
            return res;
        }

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_quic_transport_params")]
        internal static extern int SslSetQuicTransportParams(IntPtr ssl, byte* param, IntPtr length);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_get_peer_quic_transport_params")]
        internal static extern int SslGetPeerQuicTransportParams(IntPtr ssl, out byte* param, out IntPtr length);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_quic_max_handshake_flight_len")]
        internal static extern int SslQuicMaxHandshakeFlightLen(IntPtr ssl, OpenSslEncryptionLevel level);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_quic_read_level")]
        internal static extern OpenSslEncryptionLevel SslQuicReadLevel(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_quic_write_level")]
        internal static extern OpenSslEncryptionLevel SslQuicWriteLevel(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_is_init_finished")]
        internal static extern int SslIsInitFinished(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_get_current_cipher")]
        internal static extern IntPtr SslGetCurrentCipher(IntPtr ssl);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_CIPHER_get_protocol_id")]
        internal static extern ushort SslCipherGetProtocolId(IntPtr cipher);

        internal static TlsCipherSuite SslGetCipherId(IntPtr ssl)
        {
            var cipher = SslGetCurrentCipher(ssl);
            return (TlsCipherSuite)SslCipherGetProtocolId(cipher);
        }

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_ciphersuites")]
        internal static extern int SslSetCiphersuites(IntPtr ssl, byte* list);

        internal static int SslSetCiphersuites(IntPtr ssl, string list)
        {
            var ptr = Marshal.StringToHGlobalAnsi(list);
            int result = SslSetCiphersuites(ssl, (byte*) ptr.ToPointer());
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_cipher_list")]
        internal static extern int SslSetCipherList(IntPtr ssl, byte* list);

        internal static int SslSetCipherList(IntPtr ssl, string list)
        {
            var ptr = Marshal.StringToHGlobalAnsi(list);
            int result = SslSetCipherList(ssl, (byte*) ptr.ToPointer());
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_get_cipher_list")]
        internal static extern IntPtr SslGetCipherList(IntPtr ssl, int priority);

        internal static List<string> SslGetCipherList(IntPtr ssl)
        {
            var list = new List<string>();

            int priority = 0;
            IntPtr ptr;
            while ((ptr = SslGetCipherList(ssl, priority)) != IntPtr.Zero)
            {
                list.Add(Marshal.PtrToStringAnsi(ptr)!);
                priority++;
            }

            return list;
        }

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_set_alpn_protos")]
        internal static extern int SslSetAlpnProtos(IntPtr ssl, IntPtr protosStr, int protosLen);

        [DllImport(Libraries.Ssl, EntryPoint = "SSL_get0_alpn_selected")]
        internal static extern int SslGet0AlpnSelected(IntPtr ssl, out IntPtr data, out int len);
    }
}
