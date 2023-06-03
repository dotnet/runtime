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
using System.Threading;
using System.Text;
using System.IO;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Tracing.Tests.DiagnosticPortValidation
{
    public class DiagnosticPortValidation
    {
        private static readonly int s_NumberOfPorts = 4;
        public static async Task<bool> TEST_MultipleConnectPortsNoSuspend()
        {
            bool fSuccess = true;
            var serverAndNames = new List<(ReverseServer, string)>();
            string dotnetDiagnosticPorts = "";
            for (int i = 0; i < s_NumberOfPorts; i++)
            {
                string serverName = ReverseServer.MakeServerAddress();
                var server = new ReverseServer(serverName);
                Logger.logger.Log($"Server {i} address is '{serverName}'");
                serverAndNames.Add((server, serverName));
                dotnetDiagnosticPorts += $"{serverName},nosuspend;";
            }
            Logger.logger.Log($"export DOTNET_DiagnosticPorts={dotnetDiagnosticPorts}");
            var advertisements = new List<IpcAdvertise>();
            Object sync = new Object();
            int subprocessId = -1;
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string>
                {
                    { Utils.DiagnosticPortsEnvKey, dotnetDiagnosticPorts }
                },
                duringExecution: async (int pid) =>
                {
                    subprocessId = pid;
                    var tasks = new List<Task>();
                    for (int i = 0; i < s_NumberOfPorts; i++)
                    {
                        var (server, _) = serverAndNames[i];
                        int serverIndex = i;
                        tasks.Add(Task.Run(async () =>
                        {
                            Stream stream = await server.AcceptAsync();
                            IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                            lock(sync)
                                advertisements.Add(advertise);
                            Logger.logger.Log($"Server {serverIndex} got advertise {advertise.ToString()}");
                        }));
                    }

                    await Task.WhenAll(tasks);
                }
            );

            fSuccess &= await subprocessTask;

            foreach (var (server, _) in serverAndNames)
                server.Shutdown();

            Guid referenceCookie = advertisements[0].RuntimeInstanceCookie;
            foreach (var adv in advertisements)
            {
                fSuccess &= (int)adv.ProcessId == subprocessId;
                fSuccess &= adv.RuntimeInstanceCookie.Equals(referenceCookie);
            }

            return fSuccess;
        }

        public static async Task<bool> TEST_MultipleConnectPortsSuspend()
        {
            bool fSuccess = true;
            var serverAndNames = new List<(ReverseServer, string)>();
            string dotnetDiagnosticPorts = "";
            for (int i = 0; i < s_NumberOfPorts; i++)
            {
                string serverName = ReverseServer.MakeServerAddress();
                var server = new ReverseServer(serverName);
                Logger.logger.Log($"Server {i} address is '{serverName}'");
                serverAndNames.Add((server, serverName));
                dotnetDiagnosticPorts += $"{serverName};";
            }
            Logger.logger.Log($"export DOTNET_DiagnosticPorts={dotnetDiagnosticPorts}");

            var advertisements = new List<IpcAdvertise>();
            Object sync = new Object();

            int subprocessId = -1;
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string>
                {
                    { Utils.DiagnosticPortsEnvKey, dotnetDiagnosticPorts }
                },
                duringExecution: async (int pid) =>
                {
                    subprocessId = pid;

                    var mre = new ManualResetEvent(false);
                    await ConfigureAndWaitForResumeSignal(pid, mre, async () =>
                    {
                        for (int i = 0; i < s_NumberOfPorts; i++)
                        {
                            fSuccess &= !mre.WaitOne(0);
                            Logger.logger.Log($"Runtime HAS NOT resumed (expects: true): {fSuccess}");
                            var (server, _) = serverAndNames[i];
                            int serverIndex = i;
                            Stream stream = await server.AcceptAsync();
                            IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                            lock(sync)
                                advertisements.Add(advertise);
                            Logger.logger.Log($"Server {serverIndex} got advertise {advertise.ToString()}");

                            // send resume command on this connection
                            var message = new IpcMessage(0x04,0x01);
                            Logger.logger.Log($"Port {serverIndex} sent: {message.ToString()}");
                            IpcMessage response = IpcClient.SendMessage(stream, message);
                            Logger.logger.Log($"Port {serverIndex} received: {response.ToString()}");
                        }
                    });

                    // runtime should have resumed now
                    fSuccess &= mre.WaitOne(0);
                    Logger.logger.Log($"Runtime HAS resumed (expects: true): {fSuccess}");

                }
            );


            fSuccess &= await subprocessTask;
            foreach (var (server, _) in serverAndNames)
                server.Shutdown();

            if (advertisements.Count() > 0)
            {
                Guid referenceCookie = advertisements[0].RuntimeInstanceCookie;
                foreach (var adv in advertisements)
                {
                    fSuccess &= (int)adv.ProcessId == subprocessId;
                    fSuccess &= adv.RuntimeInstanceCookie.Equals(referenceCookie);
                }
            }
            else
            {
                fSuccess &= false;
            }

            return fSuccess;
        }

        public static async Task<bool> TEST_SuspendDefaultPort()
        {
            bool fSuccess = true;

            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string>
                {
                    { Utils.DiagnosticPortSuspend, "1" }
                },
                duringExecution: async (int pid) =>
                {
                    var mre = new ManualResetEvent(false);
                    await ConfigureAndWaitForResumeSignal(pid, mre, () =>
                    {
                        fSuccess &= !mre.WaitOne(0);
                        Logger.logger.Log($"Runtime HAS NOT resumed (expects: true): {fSuccess}");

                        // send resume command on this connection
                        var message = new IpcMessage(0x04,0x01);
                        Logger.logger.Log($"Sent: {message.ToString()}");
                        IpcMessage response = IpcClient.SendMessage(ConnectionHelper.GetStandardTransport(pid), message);
                        Logger.logger.Log($"Received: {response.ToString()}");
                        return Task.CompletedTask;
                    });

                    // runtime should have resumed now
                    fSuccess &= mre.WaitOne(0);
                    Logger.logger.Log($"Runtime HAS resumed (expects: true): {fSuccess}");
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        private static async Task ConfigureAndWaitForResumeSignal(int pid, ManualResetEvent mre, Func<Task> resumeRuntime)
        {
            var providers = new List<Provider>(2);
            if (TestLibrary.Utilities.IsNativeAot)
            {
                // Native AOT doesn't use the private provider / EEStartupStart event, so the subprocess
                // writes a sentinel event to signal that the runtime has resumed and run the application 
                providers.Add(new Provider(nameof(SentinelEventSource)));
            }
            else
            {
                providers.Add(new Provider("Microsoft-Windows-DotNETRuntimePrivate", 0x80000000, EventLevel.Verbose));
            }

            // workaround for https://github.com/dotnet/runtime/issues/44072 which happens because the
            // above provider only sends 2 events and that can cause EventPipeEventSource (from TraceEvent)
            // to not dispatch the events if the EventBlock is a size not divisible by 8 (the reading alignment in TraceEvent).
            // Adding this provider keeps data flowing over the pipe so the reader doesn't get stuck waiting for data
            // that won't come otherwise.
            providers.Add(new Provider("Microsoft-DotNETCore-SampleProfiler"));

            // Create an eventpipe session with prodiers that tell us
            // the runtime has been resumed.  This should only happen
            // AFTER all suspend ports have sent the resume command.
            var config = new SessionConfiguration(
                circularBufferSizeMB: 1000,
                format: EventPipeSerializationFormat.NetTrace,
                providers: providers);
            Logger.logger.Log("Starting EventPipeSession over standard connection");
            using Stream eventStream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
            Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");

            Task readerTask = Task.Run(async () =>
            {
                Logger.logger.Log($"Creating EventPipeEventSource");
                using var source = new EventPipeEventSource(eventStream);
                ClrPrivateTraceEventParser parser;
                if (TestLibrary.Utilities.IsNativeAot)
                {
                    source.Dynamic.All += (eventData) =>
                    {
                        if (eventData.ProviderName == nameof(SentinelEventSource))
                            mre.Set();
                    };
                }
                else
                {
                    parser = new ClrPrivateTraceEventParser(source);
                    parser.StartupEEStartupStart += (eventData) => mre.Set();
                }

                Logger.logger.Log($"Starting processing");
                await Task.Run(() => source.Process());
                Logger.logger.Log($"Finished processing");
            });

            await resumeRuntime();

            string resumeEventName = TestLibrary.Utilities.IsNativeAot ? nameof(SentinelEventSource) : "EEStartupStart";
            Logger.logger.Log($"Waiting for runtime resume signal event ({resumeEventName})");
            mre.WaitOne();
            Logger.logger.Log($"Saw runtime resume signal event!");

            Logger.logger.Log($"Stopping EventPipeSession");
            EventPipeClient.StopTracing(pid, sessionId);
            await readerTask;
            Logger.logger.Log($"Stopped EventPipeSession");
        }

        public static async Task<bool> TEST_AdvertiseAndProcessInfoCookiesMatch()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream = new MemoryStream();
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> { { Utils.DiagnosticPortsEnvKey, $"{serverName},nosuspend" } },
                duringExecution: async (pid) =>
                {
                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    Logger.logger.Log($"Send ProcessInfo Diagnostics IPC Command");
                    // send ProcessInfo command (0x04=ProcessCommandSet, 0x00=ProcessInfo commandid)
                    var message = new IpcMessage(0x04,0x00);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                    ProcessInfo info = ProcessInfo.TryParse(response.Payload);
                    Logger.logger.Log($"ProcessInfo: {{ id={info.ProcessId}, cookie={info.RuntimeCookie}, cmdline={info.Commandline}, OS={info.OS}, arch={info.Arch} }}");

                    Utils.Assert(info.RuntimeCookie.Equals(advertise.RuntimeInstanceCookie), $"The runtime cookie reported by ProcessInfo and Advertise must match.  ProcessInfo: {info.RuntimeCookie.ToString()}, Advertise: {advertise.RuntimeInstanceCookie.ToString()}");
                    Logger.logger.Log($"ProcessInfo and Advertise Cookies are equal");
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public static async Task<bool> TEST_ConfigValidation()
        {
            // load the env var with good and bad configs.  Operation of good configs shouldn't be impeded by bad ones.
            // This test assumes all good configs have a server at the other end of the specified path.
            // Note that while a bad config might not crash the application, it may still degrade the process, e.g.,
            // a bad configuration that specifies at least a path, will most likely still be built and consume resources polling
            // for a server that won't exist.
            bool fSuccess = true;
            var serverAndNames = new List<(ReverseServer, string)>();
            string dotnetDiagnosticPorts = "";
            // TODO: Make sure these don't hang the test when the default is suspend
            dotnetDiagnosticPorts += ";;;;;;"; // empty configs shouldn't cause a crash
            dotnetDiagnosticPorts += "  ; ; ; ; ; ; ; ; ;"; // whitespace only configs shouldn't cause a crash
            dotnetDiagnosticPorts += " , , , , , ,;,,,,,;;"; // whitespace configs and empty tags with no path shouldn't cause a crash
            dotnetDiagnosticPorts += "connect,connect,connect,nosuspend,nosuspend,nosuspend,,,;"; // path that is the same as a tag name and duplicate tags shouldn't cause a crash
            dotnetDiagnosticPorts += "SomeRandomPath,nosuspend,suspend,suspend,suspend,suspend;"; // only the first tag from a pair is respected (this should result in a nosuspend port)
            dotnetDiagnosticPorts += "%%bad_Path^* fasdf----##2~~,bad tag$$@#@%_)*)@!#(&%.>,   , , , ,nosuspend,:::;"; // invalid path chars and tag chars won't cause a crash
            for (int i = 0; i < s_NumberOfPorts; i++)
            {
                string serverName = ReverseServer.MakeServerAddress();
                var server = new ReverseServer(serverName);
                Logger.logger.Log($"Server {i} address is '{serverName}'");
                serverAndNames.Add((server, serverName));
                dotnetDiagnosticPorts += $"{serverName},nosuspend;";
                dotnetDiagnosticPorts += $"{serverName},nosuspend;"; // duplicating port configs shouldn't cause issues
            }
            Logger.logger.Log($"export DOTNET_DiagnosticPorts={dotnetDiagnosticPorts}");
            var advertisements = new List<IpcAdvertise>();
            Object sync = new Object();
            int subprocessId = -1;
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string>
                {
                    { Utils.DiagnosticPortsEnvKey, dotnetDiagnosticPorts }
                },
                duringExecution: async (int pid) =>
                {
                    subprocessId = pid;
                    var tasks = new List<Task>();
                    for (int i = 0; i < s_NumberOfPorts; i++)
                    {
                        var (server, _) = serverAndNames[i];
                        int serverIndex = i;
                        tasks.Add(Task.Run(async () =>
                        {
                            Stream stream = await server.AcceptAsync();
                            IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                            lock(sync)
                                advertisements.Add(advertise);
                            Logger.logger.Log($"Server {serverIndex} got advertise {advertise.ToString()}");
                        }));
                    }

                    await Task.WhenAll(tasks);
                }
            );

            fSuccess &= await subprocessTask;

            foreach (var (server, _) in serverAndNames)
                server.Shutdown();

            Guid referenceCookie = advertisements[0].RuntimeInstanceCookie;
            foreach (var adv in advertisements)
            {
                fSuccess &= (int)adv.ProcessId == subprocessId;
                fSuccess &= adv.RuntimeInstanceCookie.Equals(referenceCookie);
            }

            return fSuccess;
        }

        public static async Task<bool> TEST_CanGetProcessInfo2WhileSuspended()
        {
            bool fSuccess = true;
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string>
                {
                    { Utils.DiagnosticPortSuspend, "1" }
                },
                duringExecution: (int pid) =>
                {
                    Stream stream = ConnectionHelper.GetStandardTransport(pid);

                    // 0x04 = ProcessCommandSet, 0x04 = ProcessInfo2
                    var processInfoMessage = new IpcMessage(0x04, 0x04);
                    Logger.logger.Log($"Wrote: {processInfoMessage}");
                    IpcMessage response = IpcClient.SendMessage(stream, processInfoMessage);
                    Logger.logger.Log($"Received: [{response.Payload.Select(b => b.ToString("X2") + " ").Aggregate(string.Concat)}]");
                    ProcessInfo2 processInfo2 = ProcessInfo2.TryParse(response.Payload);

                    if (TestLibrary.Utilities.IsMonoRuntime)
                    {
                        // Mono currently returns empty string if the runtime is suspended before an assembly is loaded
                        Utils.Assert(string.IsNullOrEmpty(processInfo2.ManagedEntrypointAssemblyName));
                    }
                    else if (TestLibrary.Utilities.IsNativeAot)
                    {
                        // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
                        // https://github.com/dotnet/runtime/issues/83051
                        // NativeAOT currently always returns empty string
                        Utils.Assert(processInfo2.ManagedEntrypointAssemblyName == string.Empty);
                    }
                    else
                    {
                        // Assembly has not been loaded yet, so the assembly file name is used
                        string expectedName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
                        Utils.Assert(expectedName.Equals(processInfo2.ManagedEntrypointAssemblyName),
                            $"ManagedEntrypointAssemblyName must match. Expected: {expectedName}, Received: {processInfo2.ManagedEntrypointAssemblyName}");
                    }

                    // send resume command on this connection
                    var message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    response = IpcClient.SendMessage(ConnectionHelper.GetStandardTransport(pid), message);
                    Logger.logger.Log($"Received: {response.ToString()}");

                    return Task.FromResult(true);
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public sealed class SentinelEventSource : EventSource
        {
            private SentinelEventSource() {}
            public static SentinelEventSource Log = new SentinelEventSource();
            public void SentinelEvent() { WriteEvent(1, nameof(SentinelEvent)); }
        }

        public static async Task<int> Main(string[] args)
        {
            if (args.Length >= 1)
            {
                // Native AOT test uses this event source as a signal that the runtime has resumed and gone on to run the application 
                if (TestLibrary.Utilities.IsNativeAot)
                    SentinelEventSource.Log.SentinelEvent();

                Console.Out.WriteLine("Subprocess started!  Waiting for input...");
                var input = Console.In.ReadLine(); // will block until data is sent across stdin
                Console.Out.WriteLine($"Received '{input}'.  Exiting...");
                return 0;
            }

            bool fSuccess = true;
            if (!IpcTraceTest.EnsureCleanEnvironment())
                return -1;
            IEnumerable<MethodInfo> tests = typeof(DiagnosticPortValidation).GetMethods().Where(mi => mi.Name.StartsWith("TEST_"));
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
