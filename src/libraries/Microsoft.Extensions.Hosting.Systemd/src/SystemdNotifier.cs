// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Hosting.Systemd
{
    [UnsupportedOSPlatform("browser")]
    public class SystemdNotifier : ISystemdNotifier
    {
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
            string socketPath = Environment.GetEnvironmentVariable(SystemdHelpers.NOTIFY_SOCKET_ENVVAR_KEY);

            if (string.IsNullOrEmpty(socketPath))
            {
                return null;
            }

            // Support abstract socket paths.
            if (socketPath[0] == '@')
            {
                socketPath = string.Create(socketPath.Length, socketPath, (buffer, state) =>
                {
                    buffer[0] = '\0';
                    state.AsSpan(1).CopyTo(buffer.Slice(1));
                });
            }

            return socketPath;
        }
    }
}
