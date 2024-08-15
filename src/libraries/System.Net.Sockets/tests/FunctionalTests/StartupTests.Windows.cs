// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        public static async Task OSSupportsIPv4()
        {
            bool parentSupported = Socket.OSSupportsIPv4;
            await RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.OSSupportsIPv4);
            }, parentSupported.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task OSSupportsIPv6()
        {
            bool parentSupported = Socket.OSSupportsIPv6;
            await RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.OSSupportsIPv6);
            }, parentSupported.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task OSSupportsUnixDomainSockets()
        {
            bool parentSupported = Socket.OSSupportsUnixDomainSockets;
            await RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.OSSupportsUnixDomainSockets);
            }, parentSupported.ToString()).DisposeAsync();
        }

#pragma warning disable CS0618 // SupportsIPv4 and SupportsIPv6 are obsolete
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task SupportsIPv4()
        {
            bool parentSupported = Socket.SupportsIPv4;
            await RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.SupportsIPv4);
            }, parentSupported.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task SupportsIPv6()
        {
            bool parentSupported = Socket.SupportsIPv6;
            await RemoteExecutor.Invoke(parentSupported =>
            {
                Assert.Equal(bool.Parse(parentSupported), Socket.SupportsIPv6);
            }, parentSupported.ToString()).DisposeAsync();
        }
#pragma warning restore CS0618

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task Ctor_SocketType_ProtocolType()
        {
            await RemoteExecutor.Invoke(() =>
            {
                new Socket(SocketType.Stream, ProtocolType.Tcp).Dispose();
            }).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task Ctor_AddressFamily_SocketType_ProtocolType()
        {
            await RemoteExecutor.Invoke(() =>
            {
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp).Dispose();
            }).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task Ctor_SafeHandle() => await RemoteExecutor.Invoke(() =>
        {
            using var pipe = new AnonymousPipeServerStream();
            using SafeHandle clientSafeHandle = pipe.ClientSafePipeHandle;
            SocketException se = Assert.Throws<SocketException>(() => new Socket(new SafeSocketHandle(clientSafeHandle.DangerousGetHandle(), ownsHandle: false)));
            Assert.Equal(SocketError.NotSocket, se.SocketErrorCode);
        }).DisposeAsync();
    }
}
