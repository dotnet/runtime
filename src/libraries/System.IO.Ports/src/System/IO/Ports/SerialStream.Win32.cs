// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Ports
{
    internal sealed partial class SerialStream
    {
        private static SafeFileHandle OpenPort(uint portNumber)
        {
            return Interop.Kernel32.CreateFile(
                @"\\?\COM" + portNumber.ToString(CultureInfo.InvariantCulture),
                Interop.Kernel32.GenericOperations.GENERIC_READ | Interop.Kernel32.GenericOperations.GENERIC_WRITE,
                FileShare.None, // comm devices must be opened w/exclusive-access
                FileMode.Open,  // comm devices must use OPEN_EXISTING
                Interop.Kernel32.FileOperations.FILE_FLAG_OVERLAPPED);
        }
    }
}
