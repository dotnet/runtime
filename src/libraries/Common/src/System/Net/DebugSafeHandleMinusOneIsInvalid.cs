// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

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
            _trace = "WARNING! GC-ed  >>" + this.GetType().FullName + "<< (should be explicitly closed) \r\n";
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Creating SafeHandle");
        }

        ~DebugSafeHandleMinusOneIsInvalid()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, _trace);
        }
    }
#endif // DEBUG
}
