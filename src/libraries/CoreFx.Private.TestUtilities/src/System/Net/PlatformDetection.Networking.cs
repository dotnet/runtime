// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class PlatformDetection
    {
        // Windows 10 Insider Preview Build 16215 introduced the necessary APIs for the UAP version of
        // ClientWebSocket.ReceiveAsync to consume partial message data as it arrives, without having to wait
        // for "end of message" to be signaled.
        public static bool ClientWebSocketPartialMessagesSupported => !IsUap || IsWindows10Version1709OrGreater;
    }
}
