// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Tracing.Tests.Common;
using System.Text;
using System.Threading;
using System.IO;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.Tests.ReverseValidation
{
    public class ReverseValidation
    {
        public static async Task<bool> TEST_RuntimeIsResilientToServerClosing()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticPortsEnvKey, $"{serverName},nosuspend" }
                },
                duringExecution: async (_) =>
                {
                    var ad1 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad1.ToString());
                    var ad2 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad2.ToString());
                    var ad3 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad3.ToString());
                    var ad4 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad4.ToString());
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public static async Task<bool> TEST_RuntimeConnectsToExistingServer()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Task<IpcAdvertise> advertiseTask = ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
            Logger.logger.Log($"Server name is `{serverName}`");
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticPortsEnvKey, $"{serverName},nosuspend" }
                },
                duringExecution: async (_) => 
                {
                    IpcAdvertise advertise = await advertiseTask;
                    Logger.logger.Log(advertise.ToString());
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }


        public static async Task<bool> TEST_CanConnectServerAndClientAtSameTime()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticPortsEnvKey, $"{serverName},nosuspend" }
                },
                duringExecution: async (int pid) =>
                {
                    Task reverseTask = Task.Run(async () => 
                    {
                        Logger.logger.Log($"Waiting for reverse connection");
                        Stream reverseStream = await server.AcceptAsync();
                        Logger.logger.Log("Got reverse connection");
                        IpcAdvertise advertise = IpcAdvertise.Parse(reverseStream);
                        Logger.logger.Log(advertise.ToString());
                    });

                    Task regularTask = Task.Run(async () => 
                    {
                        var config = new SessionConfiguration(
                            circularBufferSizeMB: 1000,
                            format: EventPipeSerializationFormat.NetTrace,
                            providers: new List<Provider> { 
                                new Provider("Microsoft-DotNETCore-SampleProfiler")
                            });
                        Logger.logger.Log("Starting EventPipeSession over standard connection");
                        using Stream stream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                        Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");
                        using var source = new EventPipeEventSource(stream);
                        Task readerTask = Task.Run(() => source.Process());
                        await Task.Delay(500);
                        Logger.logger.Log("Stopping EventPipeSession over standard connection");
                        EventPipeClient.StopTracing(pid, sessionId);
                        await readerTask;
                        Logger.logger.Log("Stopped EventPipeSession over standard connection");
                    });

                    await Task.WhenAll(reverseTask, regularTask);
                }
            );

            fSuccess &= await subprocessTask;
            server.Shutdown();

            return fSuccess;
        }

        public static async Task<bool> TEST_ServerWorksIfClientDoesntAccept()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticPortsEnvKey, $"{serverName},nosuspend" }
                },
                duringExecution: async (int pid) =>
                {
                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 10,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-DotNETCore-SampleProfiler")
                        });
                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream stream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");
                    using var source = new EventPipeEventSource(stream);
                    Task readerTask = Task.Run(() => source.Process());
                    await Task.Delay(500);
                    Logger.logger.Log("Stopping EventPipeSession over standard connection");
                    EventPipeClient.StopTracing(pid, sessionId);
                    await readerTask;
                    Logger.logger.Log("Stopped EventPipeSession over standard connection");
                }
            );

            fSuccess &= await subprocessTask;
            server.Shutdown();

            return true;
        }

        public static async Task<bool> TEST_ServerIsResilientToNoBufferAgent()
        {
            bool fSuccess = true;
            // N.B. - this test is only testing behavior on Windows since Unix Domain Sockets get their buffer size from the
            // system configuration and isn't set here.  Tests passing on Windows should indicate it would pass on Unix systems as well.
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName, 0);
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticPortsEnvKey, $"{serverName},nosuspend" }
                },
                duringExecution: async (int pid) =>
                {
                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 10,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-DotNETCore-SampleProfiler")
                        });
                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream stream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");
                    using var source = new EventPipeEventSource(stream);
                    Task readerTask = Task.Run(() => source.Process());
                    await Task.Delay(500);
                    Logger.logger.Log("Stopping EventPipeSession over standard connection");
                    EventPipeClient.StopTracing(pid, sessionId);
                    await readerTask;
                    Logger.logger.Log("Stopped EventPipeSession over standard connection");
                }
            );

            fSuccess &= await subprocessTask;
            server.Shutdown();

            return fSuccess;
        }

        public static async Task<bool> TEST_StandardConnectionStillWorksIfReverseConnectionIsBroken()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticPortsEnvKey, $"{serverName},nosuspend" }
                },
                duringExecution: async (int pid) =>
                {
                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 1000,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-DotNETCore-SampleProfiler")
                        });
                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream stream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");
                    using var source = new EventPipeEventSource(stream);
                    Task readerTask = Task.Run(() => source.Process());
                    await Task.Delay(500);
                    Logger.logger.Log("Stopping EventPipeSession over standard connection");
                    EventPipeClient.StopTracing(pid, sessionId);
                    await readerTask;
                    Logger.logger.Log("Stopped EventPipeSession over standard connection");
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public static async Task<int> Main(string[] args)
        {
            if (args.Length >= 1)
            {
                Console.Out.WriteLine("Subprocess started!  Waiting for input...");
                var input = Console.In.ReadLine(); // will block until data is sent across stdin
                Console.Out.WriteLine($"Received '{input}'.  Exiting...");
                return 0;
            }

            bool fSuccess = true;
            if (!IpcTraceTest.EnsureCleanEnvironment())
                return -1;
            IEnumerable<MethodInfo> tests = typeof(ReverseValidation).GetMethods().Where(mi => mi.Name.StartsWith("TEST_"));
            foreach (var test in tests)
            {
                Logger.logger.Log($"::== Running test: {test.Name}");
                bool result = true;
                try
                {
                    result = await (Task<bool>)test.Invoke(null, new object[] {});
                }
                catch (Exception e)
                {
                    result = false;
                    Logger.logger.Log(e.ToString());
                }
                fSuccess &= result;
                Logger.logger.Log($"Test passed: {result}");
                Logger.logger.Log($"");

            }
            return fSuccess ? 100 : -1;
        }
    }
}
