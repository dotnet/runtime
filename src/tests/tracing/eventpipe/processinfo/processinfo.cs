// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Tracing.Tests.ProcessInfoValidation
{
    public class ProcessInfoValidation
    {
        public static string NormalizeCommandLine(string cmdline)
        {
            // ASSUMPTION: double quotes (") and single quotes (') are used for paths with spaces
            // ASSUMPTION: This test will only have two parts to the commandline

            // check for quotes in first part
            var parts = new List<string>();
            bool isQuoted = false;
            int start = 0;

            for (int i = 0; i < cmdline.Length; i++)
            {
                if (isQuoted)
                {
                    if (cmdline[i] == '"' || cmdline[i] == '\'')
                    {
                        parts.Add(cmdline.Substring(start, i - start));
                        isQuoted = false;
                        start = i + 1;
                    }
                }
                else if (cmdline[i] == '"' || cmdline[i] == '\'')
                {
                    isQuoted = true;
                    start = i + 1;
                }
                else if (cmdline[i] == ' ')
                {
                    parts.Add(cmdline.Substring(start, i - start));
                    start = i + 1;
                }
                else if (i == cmdline.Length - 1)
                {
                    parts.Add(cmdline.Substring(start));
                }
            }

            string normalizedCommandLine = parts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => (new FileInfo(part)).FullName)
                .Aggregate((s1, s2) => string.Join(' ', s1, s2));

            // Tests are run out of /tmp on Mac and linux, but on Mac /tmp is actually a symlink that points to /private/tmp.
            // This isn't represented in the output from FileInfo.FullName unfortunately, so we'll fake that completion in that case.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && normalizedCommandLine.StartsWith("/tmp/"))
                normalizedCommandLine = "/private" + normalizedCommandLine;

            return normalizedCommandLine;
        }

        public static int Main(string[] args)
        {

            Process currentProcess = Process.GetCurrentProcess();
            int pid = currentProcess.Id;
            Logger.logger.Log($"Test PID: {pid}");

            Stream stream = ConnectionHelper.GetStandardTransport(pid);

            // 0x04 = ProcessCommandSet, 0x00 = ProcessInfo
            var processInfoMessage = new IpcMessage(0x04, 0x00);
            Logger.logger.Log($"Wrote: {processInfoMessage}");
            IpcMessage response = IpcClient.SendMessage(stream, processInfoMessage);
            Logger.logger.Log($"Received: {response}");

            Utils.Assert(response.Header.CommandSet == 0xFF, $"Response must have Server command set. Expected: 0xFF, Received: 0x{response.Header.CommandSet:X2}"); // server
            Utils.Assert(response.Header.CommandId == 0x00, $"Response must have OK command id. Expected: 0x00, Received: 0x{response.Header.CommandId:X2}"); // OK

            // Parse payload
            // uint64_t ProcessId;
            // GUID RuntimeCookie;
            // LPCWSTR CommandLine;
            // LPCWSTR OS;
            // LPCWSTR Arch;

            int totalSize = response.Payload.Length;
            Logger.logger.Log($"Total size of Payload == {totalSize} b");

            // VALIDATE PID
            int start = 0;
            int end = start + 8 /* sizeof(uint63_t) */;
            UInt64 processId = BitConverter.ToUInt64(response.Payload[start..end]);
            Utils.Assert((int)processId == pid, $"PID in process info must match. Expected: {pid}, Received: {processId}");
            Logger.logger.Log($"pid: {processId}");

            // VALIDATE RUNTIME COOKIE
            start = end;
            end = start + 16 /* sizeof(GUID) */;
            Guid runtimeCookie = new Guid(response.Payload[start..end]);
            Logger.logger.Log($"runtimeCookie: {runtimeCookie}");

            // VALIDATE COMMAND LINE
            start = end;
            end = start + 4 /* sizeof(uint32_t) */;
            UInt32 commandLineLength = BitConverter.ToUInt32(response.Payload[start..end]);
            Logger.logger.Log($"commandLineLength: {commandLineLength}");

            start = end;
            end = start + ((int)commandLineLength * sizeof(char));
            Utils.Assert(end <= totalSize, $"String end can't exceed payload size. Expected: <{totalSize}, Received: {end} (decoded length: {commandLineLength})");
            Logger.logger.Log($"commandLine bytes: [ {response.Payload[start..end].Select(b => b.ToString("X2") + " ").Aggregate(string.Concat)}]");
            string commandLine = System.Text.Encoding.Unicode.GetString(response.Payload[start..end]).TrimEnd('\0');
            Logger.logger.Log($"commandLine: \"{commandLine}\"");

            // The following logic is tailored to this specific test where the cmdline _should_ look like the following:
            // /path/to/corerun /path/to/processinfo.dll
            // or
            // "C:\path\to\CoreRun.exe" C:\path\to\processinfo.dll
            string currentProcessCommandLine = $"{currentProcess.MainModule.FileName} {System.Reflection.Assembly.GetExecutingAssembly().Location}";
            string receivedCommandLine = NormalizeCommandLine(commandLine);

            Utils.Assert(currentProcessCommandLine.Equals(receivedCommandLine, StringComparison.OrdinalIgnoreCase), $"CommandLine must match current process. Expected: {currentProcessCommandLine}, Received: {receivedCommandLine} (original: {commandLine})");

            // VALIDATE OS
            start = end;
            end = start + 4 /* sizeof(uint32_t) */;
            UInt32 OSLength = BitConverter.ToUInt32(response.Payload[start..end]);
            Logger.logger.Log($"OSLength: {OSLength}");

            start = end;
            end = start + ((int)OSLength * sizeof(char));
            Utils.Assert(end <= totalSize, $"String end can't exceed payload size. Expected: <{totalSize}, Received: {end} (decoded length: {OSLength})");
            Logger.logger.Log($"OS bytes: [ {response.Payload[start..end].Select(b => b.ToString("X2") + " ").Aggregate(string.Concat)}]");
            string OS = System.Text.Encoding.Unicode.GetString(response.Payload[start..end]).TrimEnd('\0');
            Logger.logger.Log($"OS: \"{OS}\"");

            // see eventpipeeventsource.cpp for these values
            string expectedOSValue = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                expectedOSValue = "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                expectedOSValue = "macOS";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                expectedOSValue = "Linux";
            }
            else
            {
                expectedOSValue = "Unknown";
            }

            Utils.Assert(expectedOSValue.Equals(OS), $"OS must match current Operating System. Expected: \"{expectedOSValue}\", Received: \"{OS}\"");

            // VALIDATE ARCH
            start = end;
            end = start + 4 /* sizeof(uint32_t) */;
            UInt32 archLength = BitConverter.ToUInt32(response.Payload[start..end]);
            Logger.logger.Log($"archLength: {archLength}");

            start = end;
            end = start + ((int)archLength * sizeof(char));
            Utils.Assert(end <= totalSize, $"String end can't exceed payload size. Expected: <{totalSize}, Received: {end} (decoded length: {archLength})");
            Logger.logger.Log($"arch bytes: [ {response.Payload[start..end].Select(b => b.ToString("X2") + " ").Aggregate(string.Concat)}]");
            string arch = System.Text.Encoding.Unicode.GetString(response.Payload[start..end]).TrimEnd('\0');
            Logger.logger.Log($"arch: \"{arch}\"");

            // see eventpipeeventsource.cpp for these values
            string expectedArchValue = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => "arm32",
                Architecture.Arm64 => "arm64",
                _ => "Unknown"
            };

            Utils.Assert(expectedArchValue.Equals(arch), $"OS must match current Operating System. Expected: \"{expectedArchValue}\", Received: \"{arch}\"");

            Utils.Assert(end == totalSize, $"Full payload should have been read. Expected: {totalSize}, Received: {end}");

            Logger.logger.Log($"\n{{\n\tprocessId: {processId},\n\truntimeCookie: {runtimeCookie},\n\tcommandLine: {commandLine},\n\tOS: {OS},\n\tArch: {arch}\n}}");

            return 100;
        }
    }
}