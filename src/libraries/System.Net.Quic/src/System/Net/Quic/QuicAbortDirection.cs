// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic
{
    [Flags]
    public enum QuicAbortDirection
    {
        Read = 1,
        Write = 2,
        Both = Read | Write
    }
}
