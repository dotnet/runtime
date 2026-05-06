// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    internal sealed class TcpKeepAlive
    {
        internal int Time { get; set; }
        internal int Interval { get; set; }
    }
}
