using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    internal struct SslMethod
    {
        public SslMethod Null => default(SslMethod);

        public static SslMethod Tls => Native.TLS_method();

        private readonly IntPtr _handle;

        private SslMethod(IntPtr handle)
        {
            _handle = handle;
        }

        public override string ToString()
        {
            return _handle.ToString("x");
        }

        private static class Native
        {
            [DllImport(Interop.Libraries.Ssl)]
            public static extern SslMethod TLS_method();
        }
    }
}
