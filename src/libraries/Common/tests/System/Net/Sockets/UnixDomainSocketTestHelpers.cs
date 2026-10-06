// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace System.Net.Test.Common
{
    internal static class UnixDomainSocketTestHelpers
    {
        public static string GetSocketDirectory()
        {
            string directory = Path.GetTempPath();
            if (PlatformDetection.IsiOS || PlatformDetection.IstvOS)
            {
                // Simulator app container paths can exceed the native socket path limit.
                string relativeDirectory = Path.GetRelativePath(Environment.CurrentDirectory, directory);
                if (Encoding.UTF8.GetByteCount(relativeDirectory) < Encoding.UTF8.GetByteCount(directory))
                {
                    directory = relativeDirectory;
                }
            }

            return directory;
        }

        public static void SkipIfFileSystemBindIsDenied()
        {
            if (PlatformDetection.IsiOS || PlatformDetection.IstvOS)
            {
                string path = Path.Combine(GetSocketDirectory(), Guid.NewGuid().ToString("N").Substring(0, 8));
                using Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    socket.Bind(new UnixDomainSocketEndPoint(path));
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied)
                {
                    throw new SkipTestException("The test application's sandbox does not allow binding Unix domain sockets in its temporary directory.");
                }
            }
        }
    }
}
