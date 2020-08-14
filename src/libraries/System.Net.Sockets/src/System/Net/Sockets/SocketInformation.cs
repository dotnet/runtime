// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    public struct SocketInformation
    {
        public byte[] ProtocolInformation { get; set; }
        public SocketInformationOptions Options { get; set; }

        internal void SetOption(SocketInformationOptions option, bool value)
        {
            if (value) Options |= option;
            else Options &= ~option;
        }

        internal bool GetOption(SocketInformationOptions option)
        {
            return (Options & option) == option;
        }
    }
}
