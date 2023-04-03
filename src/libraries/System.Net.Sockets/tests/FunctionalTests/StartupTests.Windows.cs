// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Sockets.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class StartupTests
    {
        // Socket functionality on Windows requires WSAStartup to have been called, and thus System.Net.Sockets
        // is responsible for doing so prior to making relevant native calls; this tests entry points.
        // RemoteExecutor is used so that the individual method is used as early in the process as possible.

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void OSSupportsIPv4()
        {
            bool parentSupported = Socket.OSSupportsIPv4;
            RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.OSSupportsIPv4);
            }, parentSupported.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void OSSupportsIPv6()
        {
            bool parentSupported = Socket.OSSupportsIPv6;
            RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.OSSupportsIPv6);
            }, parentSupported.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void OSSupportsUnixDomainSockets()
        {
            bool parentSupported = Socket.OSSupportsUnixDomainSockets;
            RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.OSSupportsUnixDomainSockets);
            }, parentSupported.ToString()).Dispose();
        }

#pragma warning disable CS0618 // SupportsIPv4 and SupportsIPv6 are obsolete
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void SupportsIPv4()
        {
            bool parentSupported = Socket.SupportsIPv4;
            RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.SupportsIPv4);
            }, parentSupported.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void SupportsIPv6()
        {
            bool parentSupported = Socket.SupportsIPv6;
            RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.SupportsIPv6);
            }, parentSupported.ToString()).Dispose();
        }
#pragma warning restore CS0618

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Ctor_SocketType_ProtocolType()
        {
            RemoteExecutor.Invoke(() =>
            {
                new Socket(SocketType.Stream, ProtocolType.Tcp).Dispose();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Ctor_AddressFamily_SocketType_ProtocolType()
        {
            RemoteExecutor.Invoke(() =>
            {
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp).Dispose();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Ctor_SafeHandle() => RemoteExecutor.Invoke(() =>
        {
            using var pipe = new AnonymousPipeServerStream();
            using SafeHandle clientSafeHandle = pipe.ClientSafePipeHandle;
            SocketException se = Assert.Throws<SocketException>(() => new Socket(new SafeSocketHandle(clientSafeHandle.DangerousGetHandle(), ownsHandle: false)));
            Assert.Equal(SocketError.NotSocket, se.SocketErrorCode);
        }).Dispose();
    }
}
