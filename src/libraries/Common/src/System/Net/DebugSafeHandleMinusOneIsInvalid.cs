// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
#if DEBUG
    //
    // This is a helper class for debugging GC-ed handles that we define.
    // As a general rule normal code path should always destroy handles explicitly
    //
    internal abstract class DebugSafeHandleMinusOneIsInvalid : SafeHandleMinusOneIsInvalid
    {
        private string _trace;

        protected DebugSafeHandleMinusOneIsInvalid(bool ownsHandle) : base(ownsHandle)
        {
            Trace();
        }

        private void Trace()
        {
            _trace = "WARNING! GC-ed  >>" + this.GetType().FullName + "<< (should be explicitly closed) \r\n";
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, "Creating SafeHandle");
#if TRACE_VERBOSE
            string stacktrace = Environment.StackTrace;
            _trace += stacktrace;
#endif //TRACE_VERBOSE
        }

        ~DebugSafeHandleMinusOneIsInvalid()
        {
            DebugThreadTracking.SetThreadSource(ThreadKinds.Finalization);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, _trace);
        }
    }
#endif // DEBUG
}
