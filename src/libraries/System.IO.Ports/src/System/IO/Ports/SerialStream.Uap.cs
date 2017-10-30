// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Ports
{
    internal sealed partial class SerialStream
    {
        public SafeFileHandle OpenPort(uint portNumber)
        {
            return Interop.mincore.OpenCommPort(
                portNumber,
                Interop.Kernel32.GenericOperations.GENERIC_READ | Interop.Kernel32.GenericOperations.GENERIC_WRITE,
                NativeMethods.FILE_FLAG_OVERLAPPED);
        }
    }
}
