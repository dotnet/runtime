// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Tracing.Tests.Common;
using System.Threading;
using System.IO;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.Tests.ReverseValidation
{
    public class ReverseValidation
    {
        // The runtime will do an exponential falloff by a factor of 1.25 starting at 10ms with a max of 500ms
        // We can time tests out after waiting 30s which should have sufficient attempts
        private static int _maxPollTimeMS = 30_000;

        private static async Task<T> WaitTillTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            if (completedTask == task)
            {
                cts.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException("Task timed out");
            }
        }

        public static async Task RunSubprocess(string serverName, Func<Task> beforeExecution = null, Func<int, Task> duringExecution = null, Func<Task> afterExecution = null)
        {
            using (var process = new Process())
            {
                if (beforeExecution != null)
                    await beforeExecution();

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Environment.Add("DOTNET_DiagnosticsMonitorAddress", serverName);
                process.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                process.StartInfo.Arguments = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath + " 0";
                Logger.logger.Log($"running sub-process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                bool fSuccess = process.Start();
                Logger.logger.Log($"subprocess started: {fSuccess}");
                Logger.logger.Log($"subprocess PID: {process.Id}");

                while (!EventPipeClient.ListAvailablePorts().Contains(process.Id))
                    await Task.Delay(100);
                try
                {
                    if (duringExecution != null)
                        await duringExecution(process.Id);
                }
                finally
                {
                    process.Kill();
                }


                if (afterExecution != null)
                    await afterExecution();
            }
        }

        public static async Task<bool> TEST_RuntimeIsResilientToServerClosing()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            await RunSubprocess(
                serverName: serverName,
                duringExecution: async (_) =>
                {
                    var ad1 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                    Logger.logger.Log(ad1.ToString());
                    var ad2 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                    Logger.logger.Log(ad2.ToString());
                    var ad3 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                    Logger.logger.Log(ad3.ToString());
                    var ad4 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                    Logger.logger.Log(ad4.ToString());
                }
            );

            return true;
        }

        public static async Task<bool> TEST_RuntimeConnectsToExistingServer()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Task<IpcAdvertise> advertiseTask = ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
            Logger.logger.Log($"Server name is `{serverName}`");
            await RunSubprocess(
                serverName: serverName,
                duringExecution: async (_) => 
                {
                    IpcAdvertise advertise = await WaitTillTimeout(advertiseTask, TimeSpan.FromMilliseconds(_maxPollTimeMS));
                    Logger.logger.Log(advertise.ToString());
                }
            );

            return true;
        }


        public static async Task<bool> TEST_CanConnectServerAndClientAtSameTime()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            await RunSubprocess(
                serverName: serverName,
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

            server.Shutdown();

            return true;
        }

        public static async Task<bool> TEST_ServerWorksIfClientDoesntAccept()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            await RunSubprocess(
                serverName: serverName,
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

            server.Shutdown();

            return true;
        }

        public static async Task<bool> TEST_ServerIsResilientToNoBufferAgent()
        {
            // N.B. - this test is only testing behavior on Windows since Unix Domain Sockets get their buffer size from the
            // system configuration and isn't set here.  Tests passing on Windows should indicate it would pass on Unix systems as well.
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName, 0);
            await RunSubprocess(
                serverName: serverName,
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

            server.Shutdown();

            return true;
        }

        public static async Task<bool> TEST_ReverseConnectionCanRecycleWhileTracing()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            await RunSubprocess(
                serverName: serverName,
                duringExecution: async (int pid) =>
                {
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

                    Task reverseTask = Task.Run(async () => 
                    {
                        var ad1 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                        Logger.logger.Log(ad1.ToString());
                        var ad2 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                        Logger.logger.Log(ad2.ToString());
                        var ad3 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                        Logger.logger.Log(ad3.ToString());
                        var ad4 = await WaitTillTimeout(ReverseServer.CreateServerAndReceiveAdvertisement(serverName), TimeSpan.FromMilliseconds(_maxPollTimeMS));
                        Logger.logger.Log(ad4.ToString());
                    });

                    await Task.WhenAll(reverseTask, regularTask);
                }
            );

            return true;
        }

        public static async Task<bool> TEST_StandardConnectionStillWorksIfReverseConnectionIsBroken()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            await RunSubprocess(
                serverName: serverName,
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

            return true;
        }

        public static async Task<int> Main(string[] args)
        {
            if (args.Length >= 1)
            {
                await Task.Delay(TimeSpan.FromMinutes(10)); // will be killed in test
                return 1;
            }

            bool fSuccess = true;
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