// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Sockets.Tests;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Test.Common
{
    // Tests are relatively long-running, and we do not expect the TestPortPool to be changed frequently
    // [OuterLoop]
    [Collection(nameof(DisableParallelExecution))]
    public class TestPortPoolTests
    {
        [CollectionDefinition(nameof(DisableParallelExecution), DisableParallelization = true)]
        public class DisableParallelExecution {}

        // Port range 25010-25470 is likely unused:
        // https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
        private const int FirstUnusedPort = 25010;

        private readonly ITestOutputHelper _output;

        public TestPortPoolTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static RemoteInvokeOptions CreateRemoteOptions(string portRangeString)
        {
            return new RemoteInvokeOptions()
            {
                StartInfo = new ProcessStartInfo()
                {
                    Environment = {["DOTNET_TEST_NET_SOCKETS_PORTPOOLRANGE"] = portRangeString}
                }
            };
        }

        [Fact]
        public static void PortRange_IsConfigurable()
        {
            RemoteInvokeOptions options = CreateRemoteOptions(" 10    142  ");

            static void RunTest()
            {
                var range = Configuration.Sockets.TestPoolPortRange;
                Assert.Equal(10, range.Min);
                Assert.Equal(142, range.Max);
            }

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [Fact]
        public static void PortRange_HasCorrectDefaults()
        {
            static void RunTest()
            {
                var range = Configuration.Sockets.TestPoolPortRange;

                Assert.True(range.Min < range.Max);
                Assert.True(range.Max < 32768);
                Assert.True(range.Min > 15000);
            }

            RemoteExecutor.Invoke(RunTest).Dispose();
        }


        [Theory]
        [InlineData(FirstUnusedPort, FirstUnusedPort + 300)]
        [InlineData(FirstUnusedPort, FirstUnusedPort + 3)]
        public static void AllPortsAreWithinRange(int minOuter, int maxOuter)
        {
            static void RunTest(string minStr, string maxStr)
            {
                int min = int.Parse(minStr);
                int max = int.Parse(maxStr);
                int rangeLength = max - min;

                HashSet<int> allVisitedValues = new HashSet<int>();
                for (long i = 0; i < rangeLength * 2 + 42; i++)
                {
                    using PortLease lease = TestPortPool.RentPort();
                    allVisitedValues.Add(lease.Port);
                }

                Assert.Equal(rangeLength, allVisitedValues.Count);
                Assert.Equal(min, allVisitedValues.Min());

                // Maximum is exclusive:
                Assert.Equal(max - 1, allVisitedValues.Max());
            }

            RemoteInvokeOptions options = CreateRemoteOptions($"{minOuter} {maxOuter}");
            RemoteExecutor.Invoke(RunTest, minOuter.ToString(), maxOuter.ToString(), options).Dispose();
        }

        [Fact]
        public void WhenExhausted_Throws()
        {
            static void RunTest()
            {
                Assert.Throws<TestPortPoolExhaustedException>(() =>
                {
                    for (int i = 0; i < 21; i++)
                    {
                        TestPortPool.RentPort();
                    }
                });
            }

            RemoteInvokeOptions options = CreateRemoteOptions($"{FirstUnusedPort} {FirstUnusedPort + 20}");
            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [Theory]
        [InlineData(1200)]
        public void ConcurrentAccess_AssignedPortsAreUnique(int portRangeLength)
        {
            const int levelOfParallelism = 8;
            const int requestPerThread = 200;
            const int maxDelayInTicks = 500;
            const int returnPortsAfterTicks = 10000;

            static async Task<int> RunTest()
            {
                Task[] workItems = new Task[levelOfParallelism];

                ConcurrentDictionary<int, int> currentPorts = new ConcurrentDictionary<int, int>();

                for (int i = 0; i < levelOfParallelism; i++)
                {
                    workItems[i] = Task.Factory.StartNew( ii =>
                    {
                        Random rnd = new Random((int)ii);

                        List<PortLease> livingAssignments = new List<PortLease>();

                        Stopwatch sw = Stopwatch.StartNew();
                        long returnPortsAfter = rnd.Next(returnPortsAfterTicks);

                        for (int j = 0; j < requestPerThread; j++)
                        {
                            Thread.Sleep(TimeSpan.FromTicks(rnd.Next(maxDelayInTicks)));

                            PortLease lease = TestPortPool.RentPort();

                            Assert.True(currentPorts.TryAdd(lease.Port, 0),
                                "Same port has been rented more than once!");

                            livingAssignments.Add(lease);

                            if (sw.ElapsedTicks > returnPortsAfter) Reset();
                        }

                        void Reset()
                        {
                            sw.Stop();

                            foreach (PortLease assignment in livingAssignments)
                            {
                                Assert.True(currentPorts.TryRemove(assignment.Port, out _));
                                assignment.Dispose();
                            }
                            livingAssignments.Clear();
                            returnPortsAfter = rnd.Next(returnPortsAfterTicks);
                            sw.Start();
                        }
                    }, i);

                }

                await Task.WhenAll(workItems);

                return RemoteExecutor.SuccessExitCode;
            }

            RemoteInvokeOptions options = CreateRemoteOptions($"{FirstUnusedPort} {FirstUnusedPort + portRangeLength}");
            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [Fact]
        public void TestSocketIntegration()
        {
            static async Task<int> RunTest()
            {
                const int levelOfParallelism = 8;
                const int requestPerThread = 200;

                Task[] workItems = Enumerable.Repeat(Task.Run(() =>
                {
                    for (int i = 0; i < requestPerThread; i++)
                    {
                        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        using PortLease lease = TestPortPool.RentPortAndBindSocket(socket, IPAddress.Loopback);

                        Assert.True(socket.IsBound);
                        IPEndPoint ep = (IPEndPoint)socket.LocalEndPoint;
                        Assert.Equal(lease.Port, ep.Port);
                    }
                }), levelOfParallelism).ToArray();

                await Task.WhenAll(workItems);
                return RemoteExecutor.SuccessExitCode;
            }

            RemoteExecutor.Invoke(RunTest).Dispose();
        }

        [Fact]
        public void PortRange_GetDefaultOsDynamicPortRange()
        {
            var r = PortRange.GetDefaultOsDynamicPortRange();
            _output.WriteLine("OS Dynamic Port Range: " + r);
            throw new Exception("OS Dynamic Port Range: " + r);
        }

        [Fact]
        public void PortRange_ParseCmdOutputWindows()
        {
            const string cmdOutput = @"
Protocol tcp Dynamic Port Range
---------------------------------
Start Port      : 49152
Number of Ports : 16384
";
            PortRange range = PortRange.ParseCmdletOutputWindows(cmdOutput);
            Assert.Equal(49152, range.Min);
            Assert.Equal(49152 + 16384, range.Max);
        }

        [Theory]
        [InlineData("32768   60999")]
        [InlineData("32768   60999\n")]
        [InlineData("32768\t\t60999\n")]
        public void PortRange_ParseCmdOutputLinux(string cmdOutput)
        {
            PortRange range = PortRange.ParseCmdletOutputLinux(cmdOutput);
            Assert.Equal(32768, range.Min);
            Assert.Equal(60999, range.Max);
        }

        [Fact]
        public void PortRange_ParseCmdOutputMac()
        {
            const string cmdOutput = @"net.inet.ip.portrange.first: 49152
net.inet.ip.portrange.last: 65535
";
            PortRange range = PortRange.ParseCmdletOutputMac(cmdOutput);
            Assert.Equal(49152, range.Min);
            Assert.Equal(65535, range.Max);
        }
    }
}
