// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests for the constructors for NamedPipeClientStream
    /// </summary>
    public class NamedPipeTest_CurrentUserOnly
    {
        [Fact]
        public static void CreateClient_CurrentUserOnly()
        {
            // Should not throw.
            new NamedPipeClientStream(".", PipeStreamConformanceTests.GetUniquePipeName(), PipeDirection.InOut, PipeOptions.CurrentUserOnly).Dispose();
        }

        [Fact]
        public static void CreateServer_CurrentUserOnly()
        {
            // Should not throw.
            new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName(), PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly).Dispose();
        }

        [Fact]
        public static void CreateServer_ConnectClient()
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();
            using (var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
            {
                using (var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.CurrentUserOnly))
                {
                    // Should not fail to connect since both, the server and client have the same owner.
                    client.Connect();
                }
            }
        }

        [Fact]
        public static void CreateServer_ConnectClient_UsingUnixAbsolutePath()
        {
            string name = Path.Combine("/tmp", PipeStreamConformanceTests.GetUniquePipeName());
            using (var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
            {
                using (var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.CurrentUserOnly))
                {
                    client.Connect();
                }
            }
        }

        [Theory]
        [InlineData(PipeOptions.None, PipeOptions.CurrentUserOnly)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.None)]
        public static void Connection_UnderSameUser_SingleSide_CurrentUserOnly_Works(PipeOptions serverPipeOptions, PipeOptions clientPipeOptions)
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();
            using (var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, serverPipeOptions))
            using (var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, clientPipeOptions))
            {
                Task[] tasks = new[]
                {
                    Task.Run(() => server.WaitForConnection()),
                    Task.Run(() => client.Connect())
                };

                Assert.True(Task.WaitAll(tasks, 20_000));
            }
        }

        [Fact]
        public static void CreateMultipleServers_ConnectMultipleClients()
        {
            string name1 = PipeStreamConformanceTests.GetUniquePipeName();
            string name2 = PipeStreamConformanceTests.GetUniquePipeName();
            string name3 = PipeStreamConformanceTests.GetUniquePipeName();
            using (var server1 = new NamedPipeServerStream(name1, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
            using (var server2 = new NamedPipeServerStream(name2, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
            using (var server3 = new NamedPipeServerStream(name3, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
            {
                using (var client1 = new NamedPipeClientStream(".", name1, PipeDirection.InOut, PipeOptions.CurrentUserOnly))
                using (var client2 = new NamedPipeClientStream(".", name2, PipeDirection.InOut, PipeOptions.CurrentUserOnly))
                using (var client3 = new NamedPipeClientStream(".", name3, PipeDirection.InOut, PipeOptions.CurrentUserOnly))
                {
                    client1.Connect();
                    client2.Connect();
                    client3.Connect();
                }
            }
        }

        [Fact]
        public static void CreateMultipleServers_ConnectMultipleClients_MultipleThreads()
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var name = PipeStreamConformanceTests.GetUniquePipeName();
                    using (var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
                    {
                        using (var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.CurrentUserOnly))
                        {
                            // Should not fail to connect since both, the server and client have the same owner.
                            client.Connect();
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [Theory]
        [InlineData(PipeOptions.CurrentUserOnly)]
        [InlineData(PipeOptions.None)]
        public static void CreateMultipleConcurrentServers_ConnectMultipleClients(PipeOptions extraPipeOptions)
        {
            var pipeServers = new NamedPipeServerStream[5];
            var pipeClients = new NamedPipeClientStream[pipeServers.Length];

            try
            {
                string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
                for (var i = 0; i < pipeServers.Length; i++)
                {
                    pipeServers[i] = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.WriteThrough | extraPipeOptions);

                    pipeClients[i] = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | extraPipeOptions);
                    pipeClients[i].Connect(15_000);
                }
            }
            finally
            {
                for (var i = 0; i < pipeServers.Length; i++)
                {
                    pipeServers[i]?.Dispose();
                    pipeClients[i]?.Dispose();
                }
            }
        }
    }
}
