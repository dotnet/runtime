// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Net.Sockets
{
    [Flags]
    public enum SocketInformationOptions
    {
        NonBlocking = 0x1,
        //Even though getpeername can give a hint that we're connected, this needs to be passed because
        //disconnect doesn't update getpeername to return a failure.
        Connected = 0x2,
        Listening = 0x4,
        [Obsolete("This flag has no effect in .NET 5+ and .NET Core.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        UseOnlyOverlappedIO = 0x8,
    }
}
