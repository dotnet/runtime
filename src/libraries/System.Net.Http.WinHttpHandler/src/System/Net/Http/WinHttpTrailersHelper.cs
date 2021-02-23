using System.Diagnostics;
using System.Runtime.InteropServices;
using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    public static class WinHttpTrailersHelper
    {
        private static Lazy<bool> s_trailersSupported = new Lazy<bool>(GetTrailersSupported);
        public static bool AreTrailersSupported => s_trailersSupported.Value;

        private static bool GetTrailersSupported()
        {
            SafeWinHttpHandle sessionHandle = null;

            try
            {
                sessionHandle = Interop.WinHttp.WinHttpOpen(
                    IntPtr.Zero,
                    Interop.WinHttp.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
                    Interop.WinHttp.WINHTTP_NO_PROXY_NAME,
                    Interop.WinHttp.WINHTTP_NO_PROXY_BYPASS,
                    (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);

                if (sessionHandle.IsInvalid) return false;
                uint buffer = 0;
                uint bufferSize = sizeof(uint);
                if (Interop.WinHttp.WinHttpQueryOption(sessionHandle, Interop.WinHttp.WINHTTP_OPTION_STREAM_ERROR_CODE, ref buffer, ref bufferSize))
                {
                    Debug.Fail("Querying WINHTTP_OPTION_STREAM_ERROR_CODE on a session handle should never succeed.");
                    return false;
                }

                int lastError = Marshal.GetLastWin32Error();
                return lastError != Interop.WinHttp.WINHTTP_INVALID_STATUS_CALLBACK;
            }
            finally
            {
                sessionHandle.Dispose();
            }
        }
    }
}
