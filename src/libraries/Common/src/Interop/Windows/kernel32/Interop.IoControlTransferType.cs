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
        /// <a href="https://docs.microsoft.com/en-us/windows-hardware/drivers/kernel/buffer-descriptions-for-i-o-control-codes">TransferType</a>.
        /// Indicates how the system will pass data between the caller of DeviceIoControl (or IoBuildDeviceIoControlRequest) and the driver that handles the IRP.
        /// </summary>
        public enum IoControlTransferType
        {
            /// <summary>
            /// Specifies the buffered I/O method, which is typically used for transferring small amounts of data per request. 
            /// Most I/O control codes for device and intermediate drivers use this TransferType value.
            /// </summary>
            METHOD_BUFFERED,
            
            /// <summary>
            /// Specifies the direct I/O method, which is typically used for reading or writing large amounts of data, using DMA or PIO, that must be transferred quickly.
            /// Specify METHOD_IN_DIRECT if the caller of DeviceIoControl or IoBuildDeviceIoControlRequest will pass data to the driver.
            /// </summary>
            METHOD_IN_DIRECT,
            
            /// <summary>
            /// Specifies the direct I/O method, which is typically used for reading or writing large amounts of data, using DMA or PIO, that must be transferred quickly.
            /// Specify METHOD_OUT_DIRECT if the caller of DeviceIoControl or IoBuildDeviceIoControlRequest will receive data from the driver.
            /// </summary>
            METHOD_OUT_DIRECT,
            
            /// <summary>
            /// Specifies neither buffered nor direct I/O. The I/O manager does not provide any system buffers or MDLs. The IRP supplies the user-mode virtual addresses 
            /// of the input and output buffers that were specified to DeviceIoControl or IoBuildDeviceIoControlRequest, without validating or mapping them.
            /// </summary>
            METHOD_NEITHER
        }
    }
}
