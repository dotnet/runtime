// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets
{
    // Provides constant values for socket messages.

    [Flags]
    public enum TransmitFileOptions
    {
        // Use no flags for this call.
        UseDefaultWorkerThread = 0x00,

        // Use no flags for this call.
        Disconnect = 0x01,

        // Use no flags for this call.
        ReuseSocket = 0x02,

        // Use no flags for this call.
        WriteBehind = 0x04,

        // Use no flags for this call.
        UseSystemThread = 0x10,

        // Use no flags for this call.
        UseKernelApc = 0x20,
    }
}
