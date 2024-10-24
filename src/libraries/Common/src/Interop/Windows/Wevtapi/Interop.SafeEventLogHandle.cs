// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        /// <summary>
        /// A SafeHandle implementation over native EVT_HANDLE
        /// obtained from EventLog Native Methods.
        /// </summary>
        internal sealed class SafeEventLogHandle : SafeHandle
        {
            public SafeEventLogHandle()
                : base(nint.Zero, true)
            {
            }

            internal SafeEventLogHandle(nint handle, bool ownsHandle)
                : base(nint.Zero, ownsHandle)
            {
                SetHandle(handle);
            }

            public override bool IsInvalid
            {
                get
                {
                    return IsClosed || handle == nint.Zero;
                }
            }

            protected override bool ReleaseHandle()
            {
                EvtClose(handle);
                handle = nint.Zero;
                return true;
            }

            // DONT compare EventLogHandle with EventLogHandle.Zero
            // use IsInvalid instead. Zero is provided where a NULL handle needed
            public static SafeEventLogHandle Zero
            {
                get
                {
                    return new SafeEventLogHandle();
                }
            }
        }
    }
}
