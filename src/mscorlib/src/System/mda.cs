// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System
{
#if MDA_SUPPORTED
    internal static class Mda
    {
        internal static class StreamWriterBufferedDataLost
        {
            // State: 0 (not queried); 1 (enabled); 2 (disabled)
            private static volatile int _enabledState;
            private static volatile int _captureAllocatedCallStackState;

            internal static bool Enabled {
                get {
                    if (_enabledState == 0) {
                        if (Mda.IsStreamWriterBufferedDataLostEnabled())
                            _enabledState = 1;
                        else
                            _enabledState = 2;
                    }

                    return (_enabledState == 1);
                }
            }

            internal static bool CaptureAllocatedCallStack {
                get {
                    if (_captureAllocatedCallStackState == 0) {
                        if (Mda.IsStreamWriterBufferedDataLostCaptureAllocatedCallStack())
                            _captureAllocatedCallStackState = 1;
                        else
                            _captureAllocatedCallStackState = 2;
                    }

                    return (_captureAllocatedCallStackState == 1);
                }
            }

            internal static void ReportError(String text) {
                Mda.ReportStreamWriterBufferedDataLost(text);
            }

        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ReportStreamWriterBufferedDataLost(String text);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsStreamWriterBufferedDataLostEnabled();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsStreamWriterBufferedDataLostCaptureAllocatedCallStack();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void MemberInfoCacheCreation();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void DateTimeInvalidLocalFormat();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsInvalidGCHandleCookieProbeEnabled();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void FireInvalidGCHandleCookieProbe(IntPtr cookie);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ReportErrorSafeHandleRelease(Exception ex);
    }
#endif
}
