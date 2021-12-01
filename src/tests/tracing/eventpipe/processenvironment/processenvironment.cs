// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using System.Reflection;

namespace Tracing.Tests.ProcessEnvironmentValidation
{
    public class ProcessEnvironmentValidation
    {
        public static int Main(string[] args)
        {
            if (args.Length >= 1)
             {
                Console.Out.WriteLine("Subprocess started!  Waiting for input...");
                var input = Console.In.ReadLine(); // will block until data is sent across stdin
                Console.Out.WriteLine($"Received '{input}'.  Exiting...");
                return 0;
            }

            var testEnvPairs = new Dictionary<string, string>
            {
                { "TESTKEY1", "TESTVAL1" },
                { "TESTKEY2", "TESTVAL2" },
                { "TESTKEY3", "__TEST__VAL=;--3" }
            };

            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: testEnvPairs,
                duringExecution: (int pid) =>
                {
                    Logger.logger.Log($"Test PID: {pid}");

                    Stream stream = ConnectionHelper.GetStandardTransport(pid);

                    // 0x04 = ProcessCommandSet, 0x02 = ProcessInfo
                    var processInfoMessage = new IpcMessage(0x04, 0x02);
                    Logger.logger.Log($"Wrote: {processInfoMessage}");
                    Stream continuationStream = IpcClient.SendMessage(stream, processInfoMessage, out IpcMessage response);
                    Logger.logger.Log($"Received: {response}");

                    Utils.Assert(response.Header.CommandSet == 0xFF, $"Response must have Server command set. Expected: 0xFF, Received: 0x{response.Header.CommandSet:X2}"); // server
                    Utils.Assert(response.Header.CommandId == 0x00, $"Response must have OK command id. Expected: 0x00, Received: 0x{response.Header.CommandId:X2}"); // OK

                    UInt32 continuationSizeInBytes = BitConverter.ToUInt32(response.Payload[0..4]);
                    Logger.logger.Log($"continuation size: {continuationSizeInBytes} bytes");
                    UInt16 future = BitConverter.ToUInt16(response.Payload[4..]);
                    Logger.logger.Log($"future value: {future}");

                    using var memoryStream = new MemoryStream();
                    Logger.logger.Log($"Starting to copy continuation");
                    continuationStream.CopyTo(memoryStream);
                    Logger.logger.Log($"Finished copying continuation");
                    byte[] envBlock = memoryStream.ToArray();
                    Logger.logger.Log($"Total bytes in continuation: {envBlock.Length}");

                    Utils.Assert(envBlock.Length == continuationSizeInBytes, $"Continuation size must equal the reported size in the payload response.  Expected: {continuationSizeInBytes} bytes, Received: {envBlock.Length} bytes");

                    // VALIDATE ENV
                    // env block is sent as Array<LPCWSTR> (length-prefixed array of length-prefixed wchar strings)
                    int start = 0;
                    int end = start + 4 /* sizeof(uint32_t) */;
                    UInt32 envCount = BitConverter.ToUInt32(envBlock[start..end]);
                    Logger.logger.Log($"envCount: {envCount}");

                    var env = new Dictionary<string,string>();
                    for (int i = 0; i < envCount; i++)
                    {
                        start = end;
                        end = start + 4 /* sizeof(uint32_t) */;
                        UInt32 pairLength = BitConverter.ToUInt32(envBlock[start..end]);

                        start = end;
                        end = start + ((int)pairLength * sizeof(char));
                        Utils.Assert(end <= envBlock.Length, $"String end can't exceed payload size. Expected: <{envBlock.Length}, Received: {end} (decoded length: {pairLength})");
                        string envPair = System.Text.Encoding.Unicode.GetString(envBlock[start..end]).TrimEnd('\0');
                        int equalsIndex = envPair.IndexOf('=');
                        env[envPair[0..equalsIndex]] = envPair[(equalsIndex+1)..];
                    }
                    Logger.logger.Log($"finished parsing env");


                    foreach (var (key, val) in testEnvPairs)
                        Utils.Assert(env.ContainsKey(key) && env[key].Equals(val), $"Did not find test environment pair in the environment block: '{key}' = '{val}'");
                    Logger.logger.Log($"Saw test values in env");

                    Utils.Assert(end == envBlock.Length, $"Full payload should have been read. Expected: {envBlock.Length}, Received: {end}");
                    return Task.FromResult(true);
                }
            );

            return subprocessTask.Result ? 100 : 0;
        }
    }
}