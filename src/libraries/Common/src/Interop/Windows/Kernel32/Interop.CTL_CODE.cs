// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Data.Common;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary> <a href="https://docs.microsoft.com/en-us/windows-hardware/drivers/kernel/defining-i-o-control-codes">CTL_CODE</a> method.</summary>
        /// <param name="deviceType">Identifies the device type. This value must match the value that is set in the DeviceType member of the driver's DEVICE_OBJECT structure.</param>
        /// <param name="function">Identifies the function to be performed by the driver. Values of less than 0x800 are reserved for Microsoft. Values of 0x800 and higher can be used by vendors.</param>
        /// <param name="method">Indicates how the system will pass data between the caller of DeviceIoControl (or IoBuildDeviceIoControlRequest) and the driver that handles the IRP.</param>
        /// <param name="access">Indicates the type of access that a caller must request when opening the file object that represents the device (see IRP_MJ_CREATE).</param>
        internal unsafe static uint CTL_CODE(
            ushort deviceType,
            ushort function,
            byte method,
            byte access)
        {
            // MaxFunctionCode specifies the maximum value of the FunctionCode field in the FSCTLs (or IOCTLs for IOCTL tests). The maximum possible value is 4095.
            // See https://docs.microsoft.com/en-us/windows-hardware/drivers/develop/how-to-select-and-configure-the-device-fundamental-tests
            if (function > 4095)
                throw ADP.ArgumentOutOfRange("function");

            return (uint)((deviceType << 16) | (access << 14) | (function << 2) | method);
        }
    }
}
