// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    [Flags]
    public enum TransmitFileOptions
    {
        UseDefaultWorkerThread = 0x00,
        Disconnect = 0x01,
        ReuseSocket = 0x02,
        WriteBehind = 0x04,
        UseSystemThread = 0x10,
        UseKernelApc = 0x20,
    };
}