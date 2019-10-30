// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// A SafeHandle implementation over native EVT_HANDLE
    /// obtained from EventLog Native Methods.
    /// </summary>
    internal sealed class EventLogHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        private EventLogHandle()
            : base(IntPtr.Zero, true)
        {
        }

        internal EventLogHandle(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid
        {
            get
            {
                return IsClosed || handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            NativeWrapper.EvtClose(handle);
            handle = IntPtr.Zero;
            return true;
        }

        // DONT compare EventLogHandle with EventLogHandle.Zero
        // use IsInvalid instead. Zero is provided where a NULL handle needed
        public static EventLogHandle Zero
        {
            get
            {
                return new EventLogHandle();
            }
        }
    }
}
