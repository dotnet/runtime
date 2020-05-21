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
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Tracing.Tests.StopOnStartValidation
{
    public class StopOnStartValidation
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

                    await Task.Delay(TimeSpan.FromSeconds(1));
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
            // source.Dynamic.All += (eventData) => Console.WriteLine($"{eventData.ProviderName} - {eventData.EventName}");
            source.Process();

            Logger.logger.Log($"isStartupEventPresent: {isStartupEventPresent}");

            return isStartupEventPresent;
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

        public static async Task<int> Main(string[] args)
        {
            if (args.Length >= 1)
            {
                await Task.Delay(TimeSpan.FromMinutes(10)); // will be killed in test
                return 1;
            }

            bool fSuccess = true;
            IEnumerable<MethodInfo> tests = typeof(StopOnStartValidation).GetMethods().Where(mi => mi.Name.StartsWith("TEST_"));
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