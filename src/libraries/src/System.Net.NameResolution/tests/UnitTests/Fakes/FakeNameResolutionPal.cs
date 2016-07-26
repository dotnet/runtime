// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace System.Net
{
    internal static class NameResolutionPal
    {
        internal static int FakesEnsureSocketsAreInitializedCallCount
        {
            get;
            private set;
        }

        internal static int FakesGetHostByNameCallCount
        {
            get;
            private set;
        }

        internal static void FakesReset()
        {
            FakesEnsureSocketsAreInitializedCallCount = 0;
            FakesGetHostByNameCallCount = 0;
        }

        internal static void EnsureSocketsAreInitialized()
        {
            FakesEnsureSocketsAreInitializedCallCount++;
        }

        internal static SocketError TryGetAddrInfo(string hostName, out IPHostEntry ipHostEntry, out int nativeErrorCode)
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
