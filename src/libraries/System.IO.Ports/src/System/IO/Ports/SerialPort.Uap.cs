// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.IO.Ports
{
    public partial class SerialPort : Component
    {
        public static string[] GetPortNames()
        {
            // See https://github.com/dotnet/corefx/issues/20588 for more information.
            throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_SerialPort_GetPortNames);
        }
    }
}
