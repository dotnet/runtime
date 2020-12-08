// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal static class NameResolutionPal
    {
        public static bool SupportsGetAddrInfoAsync => false;

        internal static int FakesGetHostByNameCallCount
        {
            get;
            private set;
        }

        internal static void FakesReset()
        {
            FakesGetHostByNameCallCount = 0;
        }

        internal static SocketError TryGetAddrInfo(string name, bool justAddresses, AddressFamily addressFamily, out string hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode)
        {
            throw new NotImplementedException();
        }

        internal static IPHostEntry GetHostByName(string hostName)
        {
            FakesGetHostByNameCallCount++;
            return null;
        }

        internal static string TryGetNameInfo(IPAddress address, out SocketError errorCode, out int nativeErrorCode)
        {
            throw new NotImplementedException();
        }

        internal static Task GetAddrInfoAsync(string hostName, bool justAddresses, AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal static IPHostEntry GetHostByAddr(IPAddress address)
        {
            throw new NotImplementedException();
        }

        internal static string GetHostName()
        {
            throw new NotImplementedException();
        }
    }
}
