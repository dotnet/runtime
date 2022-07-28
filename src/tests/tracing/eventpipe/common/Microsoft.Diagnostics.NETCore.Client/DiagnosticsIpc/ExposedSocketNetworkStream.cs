// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal sealed class ExposedSocketNetworkStream :
        NetworkStream
    {
        public ExposedSocketNetworkStream(Socket socket, bool ownsSocket)
            : base(socket, ownsSocket)
        {
        }

        public new Socket Socket => base.Socket;
    }
}
