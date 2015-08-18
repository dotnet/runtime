// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net.Sockets;

namespace System.Net.Internals
{
    // TODO #2891: Add a public ctor to SocketException instead.
    internal class InternalSocketException : SocketException
    {
        public InternalSocketException(SocketError errorCode, int platformError)
            : base((int)errorCode)
        {
            HResult = platformError;
        }
    }
}
