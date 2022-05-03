// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Tracing.Tests.Common;
using System.Threading;
using System.Text;
using System.IO;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Tracing.Tests.PauseOnStartValidation
{
    public class PauseOnStartValidation
    {
        public static async Task<bool> TEST_RuntimeResumesExecutionWithCommand()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> { { Utils.DiagnosticPortsEnvKey, $"{serverName}" } },
                duringExecution: async (_) =>
                {
                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());
                    // send ResumeRuntime command (0x04=ProcessCommandSet, 0x01=ResumeRuntime commandid)
                    var message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public static async Task<bool> TEST_TracesHaveRelevantEvents()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream = new MemoryStream();
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> { { Utils.DiagnosticPortsEnvKey, $"{serverName}" } },
                duringExecution: async (pid) =>
                {
                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 1000,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-Windows-DotNETRuntimePrivate", 0x80000000, EventLevel.Verbose)
                        });
                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");
                    Task readerTask = eventStream.CopyToAsync(memoryStream);
                    
                    Logger.logger.Log($"Send ResumeRuntime Diagnostics IPC Command");
                    // send ResumeRuntime command (0x04=ProcessCommandSet, 0x01=ResumeRuntime commandid)
                    var message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");

                    await Task.Delay(TimeSpan.FromSeconds(2));
                    Logger.logger.Log("Stopping EventPipeSession over standard connection");
                    EventPipeClient.StopTracing(pid, sessionId);
                    await readerTask;
                    Logger.logger.Log("Stopped EventPipeSession over standard connection");
                }
            );

            fSuccess &= await subprocessTask;

            memoryStream.Seek(0, SeekOrigin.Begin);
            using var source = new EventPipeEventSource(memoryStream);
            var parser = new ClrPrivateTraceEventParser(source);
            bool isStartupEventPresent= false;
            parser.StartupEEStartupStart += (eventData) => isStartupEventPresent = true;
            source.Process();

            Logger.logger.Log($"isStartupEventPresent: {isStartupEventPresent}");

            return isStartupEventPresent && fSuccess;
        }

        public static async Task<bool> TEST_MultipleSessionsCanBeStartedWhilepaused()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream1 = new MemoryStream();
            using var memoryStream2 = new MemoryStream();
            using var memoryStream3 = new MemoryStream();
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> { { Utils.DiagnosticPortsEnvKey, $"{serverName}" } },
                duringExecution: async (pid) =>
                {
                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 1000,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-Windows-DotNETRuntimePrivate", 0x80000000, EventLevel.Verbose)
                        });

                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream1 = EventPipeClient.CollectTracing(pid, config, out var sessionId1);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId1:x}");
                    Task readerTask1 = eventStream1.CopyToAsync(memoryStream1);

                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream2 = EventPipeClient.CollectTracing(pid, config, out var sessionId2);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId2:x}");
                    Task readerTask2 = eventStream2.CopyToAsync(memoryStream2);

                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream3 = EventPipeClient.CollectTracing(pid, config, out var sessionId3);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId3:x}");
                    Task readerTask3 = eventStream3.CopyToAsync(memoryStream3);

                    
                    Logger.logger.Log($"Send ResumeRuntime Diagnostics IPC Command");
                    // send ResumeRuntime command (0x04=ProcessCommandSet, 0x01=ResumeRuntime commandid)
                    var message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");

                    await Task.Delay(TimeSpan.FromSeconds(2));
                    Logger.logger.Log("Stopping EventPipeSession over standard connection");
                    EventPipeClient.StopTracing(pid, sessionId1);
                    EventPipeClient.StopTracing(pid, sessionId2);
                    EventPipeClient.StopTracing(pid, sessionId3);
                    await readerTask1;
                    await readerTask2;
                    await readerTask3;
                    Logger.logger.Log("Stopped EventPipeSession over standard connection");
                }
            );

            fSuccess &= await subprocessTask;

            int nStartupEventsSeen = 0;

            memoryStream1.Seek(0, SeekOrigin.Begin);
            using (var source = new EventPipeEventSource(memoryStream1))
            {
                var parser = new ClrPrivateTraceEventParser(source);
                parser.StartupEEStartupStart += (eventData) => nStartupEventsSeen++;
                source.Process();
            }

            memoryStream2.Seek(0, SeekOrigin.Begin);
            using (var source = new EventPipeEventSource(memoryStream2))
            {
                var parser = new ClrPrivateTraceEventParser(source);
                parser.StartupEEStartupStart += (eventData) => nStartupEventsSeen++;
                source.Process();
            }

            memoryStream3.Seek(0, SeekOrigin.Begin);
            using (var source = new EventPipeEventSource(memoryStream3))
            {
                var parser = new ClrPrivateTraceEventParser(source);
                parser.StartupEEStartupStart += (eventData) => nStartupEventsSeen++;
                source.Process();
            }

            Logger.logger.Log($"nStartupEventsSeen: {nStartupEventsSeen}");

            return nStartupEventsSeen == 3 && fSuccess;
        }

        public static async Task<bool> TEST_CanStartAndStopSessionWhilepaused()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream1 = new MemoryStream();
            using var memoryStream2 = new MemoryStream();
            using var memoryStream3 = new MemoryStream();
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> { { Utils.DiagnosticPortsEnvKey, $"{serverName}" } },
                duringExecution: async (pid) =>
                {
                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 1000,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-Windows-DotNETRuntime", UInt64.MaxValue, EventLevel.Verbose)
                        });

                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream1 = EventPipeClient.CollectTracing(pid, config, out var sessionId1);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId1:x}");
                    Task readerTask1 = eventStream1.CopyToAsync(memoryStream1);

                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream2 = EventPipeClient.CollectTracing(pid, config, out var sessionId2);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId2:x}");
                    Task readerTask2 = eventStream2.CopyToAsync(memoryStream2);

                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream3 = EventPipeClient.CollectTracing(pid, config, out var sessionId3);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId3:x}");
                    Task readerTask3 = eventStream3.CopyToAsync(memoryStream3);

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    Logger.logger.Log("Stopping EventPipeSession over standard connection");
                    EventPipeClient.StopTracing(pid, sessionId1);
                    EventPipeClient.StopTracing(pid, sessionId2);
                    EventPipeClient.StopTracing(pid, sessionId3);
                    await readerTask1;
                    await readerTask2;
                    await readerTask3;
                    Logger.logger.Log("Stopped EventPipeSession over standard connection");
                    
                    Logger.logger.Log($"Send ResumeRuntime Diagnostics IPC Command");
                    // send ResumeRuntime command (0x04=ProcessCommandSet, 0x01=ResumeRuntime commandid)
                    var message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public static async Task<bool> TEST_DisabledCommandsError()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream1 = new MemoryStream();
            using var memoryStream2 = new MemoryStream();
            using var memoryStream3 = new MemoryStream();
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> { { Utils.DiagnosticPortsEnvKey, $"{serverName}" } },
                duringExecution: async (pid) =>
                {
                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    Logger.logger.Log($"Send profiler attach Diagnostics IPC Command");
                    // send profiler attach command (0x03=ProfilerCommandId, 0x01=attach commandid)
                    var message = new IpcMessage(0x03,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                    if (response.Header.CommandSet != 0xFF && response.Header.CommandId != 0xFF)
                        throw new Exception("Command did not fail!");

                    stream = await server.AcceptAsync();
                    advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    Logger.logger.Log($"Send ResumeRuntime Diagnostics IPC Command");
                    // send ResumeRuntime command (0x04=ProcessCommandSet, 0x01=ResumeRuntime commandid)
                    message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                }
            );

            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public static async Task<bool> TEST_ProcessInfoBeforeAndAfterSuspension()
        {
            // This test only applies to platforms where the PAL is used
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true;

            // This test only applies to CoreCLR (this checks if we're running on Mono)
            if (Type.GetType("Mono.RuntimeStructs") != null)
                return true;

            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream1 = new MemoryStream();
            using var memoryStream2 = new MemoryStream();
            using var memoryStream3 = new MemoryStream();
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> { { Utils.DiagnosticPortsEnvKey, $"{serverName}" } },
                duringExecution: async (pid) =>
                {
                    Process currentProcess = Process.GetCurrentProcess();

                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    Logger.logger.Log($"Get ProcessInfo while process is suspended");
                    // 0x04 = ProcessCommandSet, 0x04 = ProcessInfo2
                    var message = new IpcMessage(0x04, 0x04);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");

                    ProcessInfo2 pi2Before = ProcessInfo2.TryParse(response.Payload);
                    Utils.Assert(pi2Before.Commandline.Equals(currentProcess.MainModule.FileName), $"Before resuming, the commandline should be the mock value of the host executable path '{currentProcess.MainModule.FileName}'. Observed: '{pi2Before.Commandline}'");

                    // recycle
                    stream = await server.AcceptAsync();
                    advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    // Start EP session to know when runtime is resumed
                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 1000,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-Windows-DotNETRuntimePrivate", 0x80000000, EventLevel.Verbose),
                            new Provider("Microsoft-DotNETCore-SampleProfiler")
                        });
                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");

                    TaskCompletionSource<bool> runtimeResumed = new(false, TaskCreationOptions.RunContinuationsAsynchronously);

                    var eventPipeTask = Task.Run(() =>
                    {
                        Logger.logger.Log("Creating source");
                        using var source = new EventPipeEventSource(eventStream);
                        var parser = new ClrPrivateTraceEventParser(source);
                        parser.StartupEEStartupStart += (_) => runtimeResumed.SetResult(true);
                        source.Process();
                        Logger.logger.Log("stopping processing");
                    });

                    Logger.logger.Log($"Send ResumeRuntime Diagnostics IPC Command");
                    // send ResumeRuntime command (0x04=ProcessCommandSet, 0x01=ResumeRuntime commandid)
                    message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");

                    // recycle
                    stream = await server.AcceptAsync();
                    advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());

                    // wait a little bit to make sure the runtime of the target is fully up, i.e., g_EEStarted == true
                    // on resource constrained CI machines this may not be instantaneous
                    Logger.logger.Log($"awaiting resume");
                    await Utils.WaitTillTimeout(runtimeResumed.Task, TimeSpan.FromSeconds(10));
                    Logger.logger.Log($"resumed");

                    // await Task.Delay(TimeSpan.FromSeconds(1));
                    Logger.logger.Log("Stopping EventPipeSession over standard connection");
                    EventPipeClient.StopTracing(pid, sessionId);
                    Logger.logger.Log($"Await reader task");
                    await eventPipeTask;
                    Logger.logger.Log("Stopped EventPipeSession over standard connection");

                    ProcessInfo2 pi2After = default;

                    // The timing is not exact. There is a small window after resuming where the mock
                    // value is still present. Retry several times to catch it.
                    var retryTask = Task.Run(async () =>
                    {
                        int i = 0;
                        do {
                            Logger.logger.Log($"Get ProcessInfo after resumption: attempt {i++}");
                            // 0x04 = ProcessCommandSet, 0x04 = ProcessInfo2
                            message = new IpcMessage(0x04, 0x04);
                            Logger.logger.Log($"Sent: {message.ToString()}");
                            response = IpcClient.SendMessage(stream, message);
                            Logger.logger.Log($"received: {response.ToString()}");

                            pi2After = ProcessInfo2.TryParse(response.Payload);

                            // recycle
                            stream = await server.AcceptAsync();
                            advertise = IpcAdvertise.Parse(stream);
                            Logger.logger.Log(advertise.ToString());
                        } while (pi2After.Commandline.Equals(pi2Before.Commandline));
                    });

                    await Utils.WaitTillTimeout(retryTask, TimeSpan.FromSeconds(10));

                    Utils.Assert(!pi2After.Commandline.Equals(pi2Before.Commandline), $"After resuming, the commandline should be the correct value. Observed: Before='{pi2Before.Commandline}' After='{pi2After.Commandline}'");
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
            IEnumerable<MethodInfo> tests = typeof(PauseOnStartValidation).GetMethods().Where(mi => mi.Name.StartsWith("TEST_"));
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
