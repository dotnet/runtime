// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;

namespace System.Net.Sockets
{
    public partial class SocketException : Win32Exception
    {
        internal SocketException(SocketError errorCode, uint platformError)
            : this(errorCode)
        {
            // platformError is unused on Windows.
        }
    }
}
