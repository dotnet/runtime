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
                dotnetDiagnosticPorts += $"{serverName},connect,nosuspend;";
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
                dotnetDiagnosticPorts += $"{serverName},connect,suspend;";
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
                    bool hasResumed = false;
                    // Create an eventpipe session that will tell us when 
                    // the EEStartupStarted event happens.  This will tell us
                    // the the runtime has been resumed.  This should only happen
                    // AFTER all suspend ports have sent the resume command.
                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 1000,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-Windows-DotNETRuntimePrivate", 0x80000000, EventLevel.Verbose)
                        });
                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");

                    Task readerTask = Task.Run(async () => 
                    {
                        Logger.logger.Log($"Creating EventPipeEventSource");
                        using var source = new EventPipeEventSource(eventStream);
                        var parser = new ClrPrivateTraceEventParser(source);
                        parser.StartupEEStartupStart += (eventData) => hasResumed = true;
                        Logger.logger.Log($"Created EventPipeEventSource");
                        Logger.logger.Log($"Starting processing");
                        await Task.Run(() => source.Process());
                        Logger.logger.Log($"Finished processing");
                    });

                    for (int i = 0; i < s_NumberOfPorts; i++)
                    {
                        fSuccess &= !hasResumed;
                        Logger.logger.Log($"Runtime is resumed (expects: false): {hasResumed}");
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

                    Logger.logger.Log($"Stopping EventPipeSession");
                    EventPipeClient.StopTracing(pid, sessionId);
                    await readerTask;
                    Logger.logger.Log($"Stopped EventPipeSession");

                    // runtime should have resumed now
                    fSuccess &= hasResumed;
                    Logger.logger.Log($"Runtime is resumed (expects: true): {hasResumed}");

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

            int subprocessId = -1;
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                { 
                    { Utils.DiagnosticPortSuspend, "1" } 
                },
                duringExecution: async (int pid) =>
                {
                    subprocessId = pid;
                    bool hasResumed = false;
                    // Create an eventpipe session that will tell us when 
                    // the EEStartupStarted event happens.  This will tell us
                    // the the runtime has been resumed.  This should only happen
                    // AFTER all suspend ports have sent the resume command.
                    var config = new SessionConfiguration(
                        circularBufferSizeMB: 1000,
                        format: EventPipeSerializationFormat.NetTrace,
                        providers: new List<Provider> { 
                            new Provider("Microsoft-Windows-DotNETRuntimePrivate", 0x80000000, EventLevel.Verbose)
                        });
                    Logger.logger.Log("Starting EventPipeSession over standard connection");
                    using Stream eventStream = EventPipeClient.CollectTracing(pid, config, out var sessionId);
                    Logger.logger.Log($"Started EventPipeSession over standard connection with session id: 0x{sessionId:x}");

                    Task readerTask = Task.Run(async () => 
                    {
                        Logger.logger.Log($"Creating EventPipeEventSource");
                        using var source = new EventPipeEventSource(eventStream);
                        var parser = new ClrPrivateTraceEventParser(source);
                        parser.StartupEEStartupStart += (eventData) => hasResumed = true;
                        Logger.logger.Log($"Created EventPipeEventSource");
                        Logger.logger.Log($"Starting processing");
                        await Task.Run(() => source.Process());
                        Logger.logger.Log($"Finished processing");
                    });


                    fSuccess &= !hasResumed;
                    Logger.logger.Log($"Runtime is resumed (expects: false): {hasResumed}");

                    // send resume command on this connection
                    var message = new IpcMessage(0x04,0x01);
                    Logger.logger.Log($"Sent: {message.ToString()}");
                    IpcMessage response = IpcClient.SendMessage(ConnectionHelper.GetStandardTransport(pid), message);
                    Logger.logger.Log($"Received: {response.ToString()}");

                    Logger.logger.Log($"Stopping EventPipeSession");
                    EventPipeClient.StopTracing(pid, sessionId);
                    await readerTask;
                    Logger.logger.Log($"Stopped EventPipeSession");

                    // runtime should have resumed now
                    fSuccess &= hasResumed;
                    Logger.logger.Log($"Runtime is resumed (expects: true): {hasResumed}");

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
