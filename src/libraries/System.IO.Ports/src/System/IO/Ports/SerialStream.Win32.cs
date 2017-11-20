// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Globalization;

namespace System.IO.Ports
{
    internal sealed partial class SerialStream
    {
        public SafeFileHandle OpenPort(uint portNumber)
        {
            return Interop.Kernel32.CreateFile(
                @"\\?\COM" + portNumber.ToString(CultureInfo.InvariantCulture),
                Interop.Kernel32.GenericOperations.GENERIC_READ | Interop.Kernel32.GenericOperations.GENERIC_WRITE,
                0,              // comm devices must be opened w/exclusive-access
                FileMode.Open,  // comm devices must use OPEN_EXISTING
                NativeMethods.FILE_FLAG_OVERLAPPED);
        }
    }
}
