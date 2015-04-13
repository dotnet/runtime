// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// This class is a wrapper around the EventStream and Create pattern.
    /// Usually, the Create pattern has the caller call Create* to allocate
    /// and CFRelease to free; however, FSEventStream has it's own release
    /// function, so we need to extend the pattern to account for that.
    /// </summary>
    [System.Security.SecurityCritical]
    internal sealed partial class SafeEventStreamHandle : SafeHandle
    {
        internal SafeEventStreamHandle(IntPtr ptr) : base(IntPtr.Zero, true)
        {
            this.SetHandle(ptr);
        }

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            Interop.EventStream.FSEventStreamStop(handle);
            Interop.EventStream.FSEventStreamInvalidate(handle);
            Interop.EventStream.FSEventStreamRelease(handle);

            return true;
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle == IntPtr.Zero;
            }
        }
    }
}
