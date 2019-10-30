// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.Net
{
#if DEBUG
    //
    // This is a helper class for debugging GC-ed handles that we define.
    // As a general rule normal code path should always destroy handles explicitly
    //
    internal abstract class DebugSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private string _trace;

        protected DebugSafeHandle(bool ownsHandle) : base(ownsHandle)
        {
            Trace();
        }

        protected DebugSafeHandle(IntPtr invalidValue, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(invalidValue);
            Trace();
        }

        private void Trace()
        {
            _trace = "WARNING! GC-ed  >>" + this.GetType().ToString() + "<< (should be explicitly closed) \r\n";
#if TRACE_VERBOSE
            _trace += Environment.StackTrace;
#endif
        }

        ~DebugSafeHandle()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, _trace);
        }
    }
#endif // DEBUG
}
