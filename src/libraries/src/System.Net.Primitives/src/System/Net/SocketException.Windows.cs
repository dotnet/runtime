// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    public partial class SocketException : Win32Exception
    {
        /// <summary>Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the default error code.</summary>
        public SocketException() : this(Marshal.GetLastWin32Error())
        {
        }

        internal SocketException(SocketError errorCode, uint platformError)
            : this(errorCode)
        {
            // platformError is unused on Windows. It's the same value as errorCode.
        }

        private static int GetNativeErrorForSocketError(SocketError error)
        {
            // SocketError values map directly to Win32 error codes
            return (int)error;
        }
    }
}
