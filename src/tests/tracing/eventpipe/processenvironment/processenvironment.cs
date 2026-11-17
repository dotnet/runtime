// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

            // Mono keeps a separate managed copy of the environment.
            if (!TestLibrary.Utilities.IsMonoRuntime)
            {
                ValidateEnvironmentSync();
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

                    Dictionary<string, string> env = GetProcessEnvironment(pid);

                    foreach (var (key, val) in testEnvPairs)
                        Utils.Assert(env.ContainsKey(key) && env[key].Equals(val), $"Did not find test environment pair in the environment block: '{key}' = '{val}'");
                    Logger.logger.Log($"Saw test values in env");

                    return Task.FromResult(true);
                }
            );

            return subprocessTask.Result ? 100 : 0;
        }

        // Variables set through the diagnostic server must be visible to Environment, and vice versa.
        private static void ValidateEnvironmentSync()
        {
            int pid = Environment.ProcessId;
            const string FromIpc = "PROCESSENVIRONMENT_FROM_IPC";
            const string FromManaged = "PROCESSENVIRONMENT_FROM_MANAGED";

            SetEnvironmentVariable(pid, FromIpc, "ipc");
            Utils.Assert(Environment.GetEnvironmentVariable(FromIpc) == "ipc", "Environment.GetEnvironmentVariable must see a variable set through the diagnostic server");
            Utils.Assert((string)Environment.GetEnvironmentVariables()[FromIpc] == "ipc", "Environment.GetEnvironmentVariables must see a variable set through the diagnostic server");

            SetEnvironmentVariable(pid, FromIpc, null);
            Utils.Assert(Environment.GetEnvironmentVariable(FromIpc) is null, "Environment.GetEnvironmentVariable must see a variable unset through the diagnostic server");

            Environment.SetEnvironmentVariable(FromManaged, "managed");
            Utils.Assert(GetProcessEnvironment(pid).TryGetValue(FromManaged, out string value) && value == "managed", "The diagnostic server must see a variable set through Environment");

            Environment.SetEnvironmentVariable(FromManaged, null);
            Utils.Assert(!GetProcessEnvironment(pid).ContainsKey(FromManaged), "The diagnostic server must see a variable unset through Environment");

            SetEnvironmentVariable(pid, FromIpc, "");
            Utils.Assert(Environment.GetEnvironmentVariable(FromIpc) == "", "A variable with an empty value must be returned as an empty string");
            SetEnvironmentVariable(pid, FromIpc, null);

            string longValue = new string('x', 1000) + "\u00e9\u4e2d\U0001F600";
            Environment.SetEnvironmentVariable(FromManaged, longValue);
            Utils.Assert(Environment.GetEnvironmentVariable(FromManaged) == longValue, "A long non-ASCII value must round-trip through Environment");
            Utils.Assert(GetProcessEnvironment(pid)[FromManaged] == longValue, "A long non-ASCII value must round-trip through the diagnostic server");

            Environment.SetEnvironmentVariable(FromManaged, "abc\0def");
            Utils.Assert(Environment.GetEnvironmentVariable(FromManaged) == "abc", "A value must be truncated at an embedded null");
            Environment.SetEnvironmentVariable(FromManaged, null);

            if (!OperatingSystem.IsWindows())
            {
                SetEnvironmentVariable(pid, FromIpc + "=INVALID", "ipc", expectSuccess: false);
                Utils.Assert(Environment.GetEnvironmentVariable(FromIpc) is null, "A variable name containing '=' must be rejected");
            }

            Logger.logger.Log("Environment is in sync between managed code and the diagnostic server");
        }

        private static void SetEnvironmentVariable(int pid, string name, string value, bool expectSuccess = true)
        {
            using var payload = new MemoryStream();
            using (var writer = new BinaryWriter(payload, Encoding.Unicode, leaveOpen: true))
            {
                WriteString(writer, name);
                WriteString(writer, value);
            }

            // 0x04 = ProcessCommandSet, 0x03 = SetEnvironmentVariable
            var message = new IpcMessage(0x04, 0x03, payload.ToArray());
            using Stream stream = ConnectionHelper.GetStandardTransport(pid);
            IpcMessage response = IpcClient.SendMessage(stream, message);

            // 0x00 = OK, 0xFF = Error
            byte expectedCommandId = expectSuccess ? (byte)0x00 : (byte)0xFF;
            Utils.Assert(response.Header.CommandSet == 0xFF, $"Response must have Server command set. Expected: 0xFF, Received: 0x{response.Header.CommandSet:X2}"); // server
            Utils.Assert(response.Header.CommandId == expectedCommandId, $"Unexpected response command id. Expected: 0x{expectedCommandId:X2}, Received: 0x{response.Header.CommandId:X2}");

            static void WriteString(BinaryWriter writer, string value)
            {
                // Length-prefixed, null-terminated UTF-16 string. A null string has length 0.
                writer.Write(value != null ? value.Length + 1 : 0);
                if (value != null)
                {
                    writer.Write(Encoding.Unicode.GetBytes(value + '\0'));
                }
            }
        }

        private static Dictionary<string, string> GetProcessEnvironment(int pid)
        {
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

            Utils.Assert(end == envBlock.Length, $"Full payload should have been read. Expected: {envBlock.Length}, Received: {end}");
            return env;
        }
    }
}
