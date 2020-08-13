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
        public static async Task<bool> TEST_ReverseConnectionCanRecycleWhileTracing()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticsMonitorAddressEnvKey, serverName },
                    { Utils.DiagnosticsMonitorPauseOnStartEnvKey, "0" }
                },
                duringExecution: async (int pid) =>
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    Task regularTask = Task.Run(async () => 
                    {
                        try
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
                            // using var source = new EventPipeEventSource(stream);
                            using var memroyStream = new MemoryStream();
                            Task readerTask = stream.CopyToAsync(memroyStream);//Task.Run(() => source.Process());
                            await Task.Delay(500);
                            Logger.logger.Log("Stopping EventPipeSession over standard connection");
                            EventPipeClient.StopTracing(pid, sessionId);
                            await readerTask;
                            Logger.logger.Log("Stopped EventPipeSession over standard connection");
                        }
                        catch (Exception e)
                        {
                            Logger.logger.Log(e.ToString());
                        }
                        finally
                        {
                            Logger.logger.Log("setting the MRE");
                            mre.Set();
                        }
                    });

                    Task reverseTask = Task.Run(async () => 
                    {
                        while (!mre.WaitOne(0))
                        {
                            var ad1 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                            Logger.logger.Log(ad1.ToString());
                        }
                    });

                    await Task.WhenAll(reverseTask, regularTask);
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
