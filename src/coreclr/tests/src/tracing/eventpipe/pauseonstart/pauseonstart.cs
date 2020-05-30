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
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            Task subprocessTask = RunSubprocess(
                serverName: serverName,
                duringExecution: async (_) =>
                {
                    Stream stream = await server.AcceptAsync();
                    IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                    Logger.logger.Log(advertise.ToString());
                    // send ResumeRuntime command (0xFF=ServerCommandSet, 0x01=ResumeRuntime commandid)
                    var message = new IpcMessage(0xFF,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                }
            );

            await WaitTillTimeout(subprocessTask, TimeSpan.FromMinutes(1));

            return true;
        }

        public static async Task<bool> TEST_TracesHaveRelevantEvents()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream = new MemoryStream();
            Task subprocessTask = RunSubprocess(
                serverName: serverName,
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
                    // send ResumeRuntime command (0xFF=ServerCommandSet, 0x01=ResumeRuntime commandid)
                    var message = new IpcMessage(0xFF,0x01);
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

            await WaitTillTimeout(subprocessTask, TimeSpan.FromMinutes(1));

            memoryStream.Seek(0, SeekOrigin.Begin);
            using var source = new EventPipeEventSource(memoryStream);
            var parser = new ClrPrivateTraceEventParser(source);
            bool isStartupEventPresent= false;
            parser.StartupEEStartupStart += (eventData) => isStartupEventPresent = true;
            source.Process();

            Logger.logger.Log($"isStartupEventPresent: {isStartupEventPresent}");

            return isStartupEventPresent;
        }

        public static async Task<bool> TEST_MultipleSessionsCanBeStartedWhilepaused()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream1 = new MemoryStream();
            using var memoryStream2 = new MemoryStream();
            using var memoryStream3 = new MemoryStream();
            Task subprocessTask = RunSubprocess(
                serverName: serverName,
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
                    // send ResumeRuntime command (0xFF=ServerCommandSet, 0x01=ResumeRuntime commandid)
                    var message = new IpcMessage(0xFF,0x01);
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

            await WaitTillTimeout(subprocessTask, TimeSpan.FromMinutes(1));

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

            return nStartupEventsSeen == 3;
        }

        public static async Task<bool> TEST_DisabledCommandsError()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            var server = new ReverseServer(serverName);
            using var memoryStream1 = new MemoryStream();
            using var memoryStream2 = new MemoryStream();
            using var memoryStream3 = new MemoryStream();
            Task subprocessTask = RunSubprocess(
                serverName: serverName,
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

                    Logger.logger.Log($"Send eventpipe stop Diagnostics IPC Command");
                    // send eventpipe stop command (0x02=EventPipe, 0x01=stop commandid)
                    message = new IpcMessage(0x02,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                    if (response.Header.CommandSet != 0xFF && response.Header.CommandId != 0xFF)
                        throw new Exception("Command did not fail!");

                    Logger.logger.Log($"Send ResumeRuntime Diagnostics IPC Command");
                    // send ResumeRuntime command (0xFF=ServerCommandSet, 0x01=ResumeRuntime commandid)
                    message = new IpcMessage(0xFF,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    response = IpcClient.SendMessage(stream, message);
                    Logger.logger.Log($"received: {response.ToString()}");
                }
            );

            await WaitTillTimeout(subprocessTask, TimeSpan.FromMinutes(1));

            return true;
        }

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

        private static async Task WaitTillTimeout(Task task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            if (completedTask == task)
            {
                cts.Cancel();
                return;
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

                var stdoutSb = new StringBuilder();
                var stderrSb = new StringBuilder();

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Environment.Add("DOTNET_DiagnosticsMonitorAddress", serverName);
                process.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                process.StartInfo.Arguments = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath + " 0";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;

                Logger.logger.Log($"running sub-process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                DateTime startTime = DateTime.Now;
                process.OutputDataReceived += new DataReceivedEventHandler((s,e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        stdoutSb.Append($"\n\t{(DateTime.Now - startTime).TotalSeconds,5:f1}s: {e.Data}");
                    }
                });

                process.ErrorDataReceived += new DataReceivedEventHandler((s,e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        stderrSb.Append($"\n\t{(DateTime.Now - startTime).TotalSeconds,5:f1}s: {e.Data}");
                    }
                });

                bool fSuccess = process.Start();
                if (!fSuccess)
                    throw new Exception("Failed to start subprocess");
                StreamWriter subprocesssStdIn = process.StandardInput;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                Logger.logger.Log($"subprocess started: {fSuccess}");
                Logger.logger.Log($"subprocess PID: {process.Id}");

                while (!EventPipeClient.ListAvailablePorts().Contains(process.Id))
                {
                    Logger.logger.Log($"Standard Diagnostics Server connection not created yet -> try again in 100 ms");
                    await Task.Delay(100);
                }

                try
                {
                    if (duringExecution != null)
                        await duringExecution(process.Id);
                    Logger.logger.Log($"Sending 'exit' to subprocess stdin");
                    subprocesssStdIn.WriteLine("exit");
                    subprocesssStdIn.Close();
                    if (!process.WaitForExit(5000))
                    {
                        Logger.logger.Log("Subprocess didn't exit in 5 seconds!");
                        throw new TimeoutException("Subprocess didn't exit in 5 seconds");
                    }
                    Logger.logger.Log($"SubProcess exited - Exit code: {process.ExitCode}");
                    Logger.logger.Log($"Subprocess stdout: {stdoutSb.ToString()}");
                    Logger.logger.Log($"Subprocess stderr: {stderrSb.ToString()}");
                }
                catch (Exception e)
                {
                    Logger.logger.Log($"Calling process.Kill()");
                    process.Kill();
                }


                if (afterExecution != null)
                    await afterExecution();
            }
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