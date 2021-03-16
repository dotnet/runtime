// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Sockets;

namespace Microsoft.Extensions.Hosting.Systemd
{
    public class SystemdNotifier : ISystemdNotifier
    {
        private const string NOTIFY_SOCKET = "NOTIFY_SOCKET";

        private readonly string _socketPath;

        public SystemdNotifier() :
            this(GetNotifySocketPath())
        { }

        // For testing
        internal SystemdNotifier(string socketPath)
        {
            _socketPath = socketPath;
        }

        /// <inheritdoc />
        public bool IsEnabled => _socketPath != null;

        /// <inheritdoc />
        public void Notify(ServiceState state)
        {
            if (!IsEnabled)
            {
                return;
            }

            using (var socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified))
            {
                var endPoint = new UnixDomainSocketEndPoint(_socketPath);
                socket.Connect(endPoint);

                // It's safe to do a non-blocking call here: messages sent here are much
                // smaller than kernel buffers so we won't get blocked.
                socket.Send(state.GetData());
            }
        }

        private static string GetNotifySocketPath()
        {
            string socketPath = Environment.GetEnvironmentVariable(NOTIFY_SOCKET);

            if (string.IsNullOrEmpty(socketPath))
            {
                return null;
            }

            // Support abstract socket paths.
            if (socketPath[0] == '@')
            {
                socketPath = "\0" + socketPath.Substring(1);
            }

            return socketPath;
        }
    }
}
