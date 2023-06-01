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
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.Tests.ApplyStartupHookValidation
{
    public class ApplyStartupHookValidation
    {
        public static async Task<bool> TEST_ApplyStartupHookAtStartupSuspension()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticPortsEnvKey, serverName }
                },
                duringExecution: async (_) =>
                {
                    ReverseServer server = new ReverseServer(serverName);
                    using (Stream stream = await server.AcceptAsync())
                    {
                        IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                        Console.WriteLine($"IpcAdvertise: {advertise}");

                        while (!Debugger.IsAttached)
                        {
                            Thread.Sleep(1000);
                        }

                        int processId = (int)advertise.ProcessId;

                        // While we are paused in startup, apply startup hook
                        DiagnosticsClient client = new DiagnosticsClient(processId);

                        string startupHookPath = Hook.Basic.AssemblyPath;
                        Console.WriteLine($"Applying startup hook during startup suspension: {startupHookPath}");
                        client.ApplyStartupHook(startupHookPath);
                    }

                    using (Stream stream = await server.AcceptAsync())
                    {
                        IpcAdvertise advertise = IpcAdvertise.Parse(stream);
                        Console.WriteLine($"IpcAdvertise: {advertise}");

                        Logger.logger.Log($"Send ResumeRuntime Diagnostics IPC Command");
                        // send ResumeRuntime command (0x04=ProcessCommandSet, 0x01=ResumeRuntime commandid)
                        IpcMessage message = new(0x04,0x01);
                        Logger.logger.Log($"Sent: {message.ToString()}");
                        IpcMessage response = IpcClient.SendMessage(stream, message);
                        Logger.logger.Log($"received: {response.ToString()}");
                    }
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
                Console.Out.WriteLine($"Received '{input}'");

                // Validate the startup hook was executed
                int callCount = Hook.Basic.CallCount;
                Console.Out.WriteLine($"Startup hook call count: {callCount}");
                return callCount > 0 ? 0 : -1;
            }

            bool fSuccess = true;
            if (!IpcTraceTest.EnsureCleanEnvironment())
                return -1;
            IEnumerable<MethodInfo> tests = typeof(ApplyStartupHookValidation).GetMethods().Where(mi => mi.Name.StartsWith("TEST_"));
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
