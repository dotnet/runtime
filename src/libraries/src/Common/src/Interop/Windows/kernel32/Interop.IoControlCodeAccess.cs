// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// <a href="https://docs.microsoft.com/en-us/windows-hardware/drivers/kernel/defining-i-o-control-codes">RequiredAccess</a>.
        /// Indicates the type of access that a caller must request when opening the file object that represents the device (see IRP_MJ_CREATE).
        /// </summary>
        [Flags]
        public enum IoControlCodeAccess
        {
            /// <summary>
            /// The I/O manager sends the IRP for any caller that has a handle to the file object that represents the target device object.
            /// </summary>
            FILE_ANY_ACCESS = 0x00,

            /// <summary>
            /// The I/O manager sends the IRP only for a caller with read access rights, allowing the underlying device driver to transfer
            /// data from the device to system memory.
            /// </summary>
            FILE_READ_DATA = 0x01,

            /// <summary>
            /// The I/O manager sends the IRP only for a caller with write access rights, allowing the underlying device driver to transfer 
            /// data from system memory to its device.
            /// </summary>
            FILE_WRITE_DATA = 0x02

            // FILE_READ_DATA and FILE_WRITE_DATA can be ORed together if the caller must have both read and write access rights.
        }
    }
}
