// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests for the constructors for NamedPipeClientStream
    /// </summary>
    public class NamedPipeTest_CurrentUserOnly : NamedPipeTestBase
    {
        [Fact]
        public static void CreateClient_CurrentUserOnly()
        {
            // Should not throw.
            new NamedPipeClientStream(".", GetUniquePipeName(), PipeDirection.InOut, PipeOptions.CurrentUserOnly).Dispose();
        }
        
        [Fact]
        public static void CreateServer_CurrentUserOnly()
        {
            // Should not throw.
            new NamedPipeServerStream(GetUniquePipeName(), PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly).Dispose();
        }

        [Fact]
        public static void CreateServer_ConnectClient()
        {
            var name = GetUniquePipeName();
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
        public static void CreateMultipleServers_ConnectMultipleClients()
        {
            var name1 = GetUniquePipeName();
            var name2 = GetUniquePipeName();
            var name3 = GetUniquePipeName();
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
                    var name = GetUniquePipeName();
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
    }
}