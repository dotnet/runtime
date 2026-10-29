// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Text;

namespace System
{
    public static partial class PlatformDetection
    {
        private static readonly Lazy<string> s_unixDomainSocketDirectory = new Lazy<string>(GetUnixDomainSocketDirectory);
        private static readonly Lazy<bool> s_supportsUnixDomainSocketBinding = new Lazy<bool>(GetSupportsUnixDomainSocketBinding);

        public static string UnixDomainSocketDirectory => s_unixDomainSocketDirectory.Value;
        public static bool SupportsUnixDomainSocketBinding => s_supportsUnixDomainSocketBinding.Value;

        private static string GetUnixDomainSocketDirectory()
        {
            string directory = Path.GetTempPath();
#if !NETFRAMEWORK
            if (IsiOS || IstvOS)
            {
                // Simulator app container paths can exceed the native socket path limit.
                string relativeDirectory = Path.GetRelativePath(Environment.CurrentDirectory, directory);
                if (Encoding.UTF8.GetByteCount(relativeDirectory) < Encoding.UTF8.GetByteCount(directory))
                {
                    directory = relativeDirectory;
                }
            }
#endif
            return directory;
        }

        private static bool GetSupportsUnixDomainSocketBinding()
        {
#if NETFRAMEWORK
            return false;
#else
            if (!Socket.OSSupportsUnixDomainSockets)
            {
                return false;
            }

            if (IsiOS || IstvOS)
            {
                string path = Path.Combine(UnixDomainSocketDirectory, Guid.NewGuid().ToString("N").Substring(0, 8));
                using Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    socket.Bind(new UnixDomainSocketEndPoint(path));
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied)
                {
                    // Filesystem socket binding can be denied by the test application's sandbox.
                    return false;
                }
            }

            return true;
#endif
        }
    }
}
