// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // TODO: Delete once https://github.com/dotnet/corefx/issues/35410 is available.
    internal sealed class ExposedSocketNetworkStream : NetworkStream
    {
        public ExposedSocketNetworkStream(Socket socket, bool ownsSocket) : base(socket, ownsSocket) { }

        public new Socket Socket => base.Socket;
    }
}
