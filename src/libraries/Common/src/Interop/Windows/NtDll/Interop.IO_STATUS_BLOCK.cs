// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff550671.aspx
        [StructLayout(LayoutKind.Sequential)]
        public struct IO_STATUS_BLOCK
        {
            /// <summary>
            /// Status
            /// </summary>
            public IO_STATUS Status;

            /// <summary>
            /// Request dependent value.
            /// </summary>
            public IntPtr Information;

            // This isn't an actual Windows type, it is a union within IO_STATUS_BLOCK. We *have* to separate it out as
            // the size of IntPtr varies by architecture and we can't specify the size at compile time to offset the
            // Information pointer in the status block.
            [StructLayout(LayoutKind.Explicit)]
            public struct IO_STATUS
            {
                /// <summary>
                /// The completion status, either STATUS_SUCCESS if the operation was completed successfully or
                /// some other informational, warning, or error status.
                /// </summary>
                [FieldOffset(0)]
                public uint Status;

                /// <summary>
                /// Reserved for internal use.
                /// </summary>
                [FieldOffset(0)]
                public IntPtr Pointer;
            }
        }
    }
}
