using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal class MsQuicSecurityConfig : IDisposable
    {
        private bool _disposed;
        private MsQuicApi _registration;

        public MsQuicSecurityConfig(MsQuicApi registration, IntPtr nativeObjPtr)
        {
            _registration = registration;
            NativeObjPtr = nativeObjPtr;
        }

        public IntPtr NativeObjPtr { get; private set; }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _registration.SecConfigDeleteDelegate?.Invoke(NativeObjPtr);
            NativeObjPtr = IntPtr.Zero;
            _disposed = true;
        }

        ~MsQuicSecurityConfig()
        {
            Dispose(disposing: false);
        }
    }
}
