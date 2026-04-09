// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal sealed class IpcUnixDomainSocket : IpcSocket
    {
        private bool _ownsSocketFile;
        private string _path;

        internal IpcUnixDomainSocket()
            : base(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)
        {
        }

        public void Bind(IpcUnixDomainSocketEndPoint localEP)
        {
            base.Bind(localEP);
            _path = localEP.Path;
            _ownsSocketFile = true;
        }

        public override void Connect(EndPoint localEP, TimeSpan timeout)
        {
            base.Connect(localEP, timeout);
            _ownsSocketFile = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_ownsSocketFile && !string.IsNullOrEmpty(_path) && File.Exists(_path))
                {
                    File.Delete(_path);
                }
            }
            base.Dispose(disposing);
        }
    }
}
