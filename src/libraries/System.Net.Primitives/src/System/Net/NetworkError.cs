// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    public enum NetworkError : int
    {
        Unknown = 0,

        // Errors from connection establishment
        AddressInUse,
        InvalidAddress,
        HostNotFound,
        ConnectionRefused,

        // Errors from connection use (e.g NetworkStream.Read/Write)
        ConnectionAborted,
        ConnectionReset,
    }
}
