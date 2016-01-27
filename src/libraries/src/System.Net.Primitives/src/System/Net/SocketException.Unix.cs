// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace System.Net.Sockets
{
    public partial class SocketException : Win32Exception
    {
        internal SocketException(SocketError errorCode, uint platformError)
            : this(errorCode)
        {
            HResult = unchecked((int)platformError);
        }
    }
}
