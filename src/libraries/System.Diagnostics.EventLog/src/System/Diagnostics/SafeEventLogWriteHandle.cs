// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEventLogWriteHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeEventLogWriteHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            return Interop.Advapi32.DeregisterEventSource(handle);
        }
    }
}
