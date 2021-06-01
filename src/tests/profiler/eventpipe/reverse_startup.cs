// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Tracing.Tests.Common;

namespace ReverseStartupTests
{
    internal static class BinaryWriterExtensions
    {
        public static void WriteString(this BinaryWriter @this, string value)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            @this.Write(value != null ? (value.Length + 1) : 0);
            if (value != null)
                @this.Write(Encoding.Unicode.GetBytes(value + '\0'));
        }

    }

    public delegate void ProfilerCallback();

    class ReverseStartup
    {
        static readonly Guid ReverseStartupProfilerGuid = new Guid("9C1A6E14-2DEC-45CE-9061-F31964D8884D");

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("In RunTest, exiting.");
                return 100;
            }

            string serverName = ReverseServer.MakeServerAddress();
            Task backgroundTask = Task.Run(() =>
            {
                ReverseServer server = null;
                try
                {
                    Task task = Task.Run(async () =>
                    {
                        server = new ReverseServer(serverName);
                        using (Stream serverStream = await server.AcceptAsync())
                        {
                            IpcAdvertise advertise = IpcAdvertise.Parse(serverStream);
                            Console.WriteLine($"Got IpcAdvertise: {advertise}");
                            
                            int processId = (int)advertise.ProcessId;

                            // While we are paused in startup send the profiler startup command
                            string profilerPath = GetProfilerPath();
                            DiagnosticsIPCWorkaround client = new DiagnosticsIPCWorkaround(processId);
                            client.SetStartupProfiler(ReverseStartupProfilerGuid, profilerPath);

                            // Resume runtime message
                            IpcMessage resumeMessage = new IpcMessage(0x04,0x01);
                            Console.WriteLine($"Sent resume runtime message: {resumeMessage.ToString()}");
                            IpcMessage resumeResponse = IpcClient.SendMessage(serverStream, resumeMessage);
                            Logger.logger.Log($"Received: {resumeResponse.ToString()}");
                        }
                    });

                    task.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ReverseServer saw exception {e.Message}");
                    Console.WriteLine(e.StackTrace);


                    Console.WriteLine($"Inner exception {e.InnerException?.Message}");
                    Console.WriteLine(e.InnerException?.StackTrace);
                    
                }
                finally
                {
                    server?.Shutdown();
                }
            });

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ReverseStartup",
                                          profilerClsid: Guid.Empty,
                                          profileeOptions: ProfileeOptions.NoStartupAttach | ProfileeOptions.ReverseDiagnosticsMode,
                                          reverseServerName: serverName);
        }

        public static string GetProfilerPath()
        {
            string profilerName;
            if (TestLibrary.Utilities.IsWindows)
            {
                profilerName = "Profiler.dll";
            }
            else if (TestLibrary.Utilities.IsLinux)
            {
                profilerName = "libProfiler.so";
            }
            else
            {
                profilerName = "libProfiler.dylib";
            }

            string rootPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string profilerPath = Path.Combine(rootPath, profilerName);

            return profilerPath;
        }
    }
}
