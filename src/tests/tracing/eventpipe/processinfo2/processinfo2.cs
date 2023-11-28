// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Xunit;

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

            StringBuilder sb = new();
            bool isArgument = false;
            for (int i = 0; i < parts.Count; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;
                else if (parts[i].StartsWith('-'))
                {
                    // if we see '-', then assume it's a '-option argument' pair and remove
                    isArgument = true;
                }
                else if (isArgument)
                {
                    isArgument = false;
                }
                else
                {
                    // assume anything else is a file/executable so get the full path
                    sb.Append((new FileInfo(parts[i])).FullName + " ");
                }
            }

            string normalizedCommandLine = sb.ToString().Trim();

            // Tests are run out of /tmp on Mac and linux, but on Mac /tmp is actually a symlink that points to /private/tmp.
            // This isn't represented in the output from FileInfo.FullName unfortunately, so we'll fake that completion in that case.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && normalizedCommandLine.StartsWith("/tmp/"))
                normalizedCommandLine = "/private" + normalizedCommandLine;

            return normalizedCommandLine;
        }

        [Fact]
        public static void TestEntryPoint()
        {

            Process currentProcess = Process.GetCurrentProcess();
            int pid = currentProcess.Id;
            Logger.logger.Log($"Test PID: {pid}");

            Stream stream = ConnectionHelper.GetStandardTransport(pid);

            // 0x04 = ProcessCommandSet, 0x04 = ProcessInfo2
            var processInfoMessage = new IpcMessage(0x04, 0x04);
            Logger.logger.Log($"Wrote: {processInfoMessage}");
            IpcMessage response = IpcClient.SendMessage(stream, processInfoMessage);
            Logger.logger.Log($"Received: <omitted>");

            Utils.Assert(response.Header.CommandSet == 0xFF, $"Response must have Server command set. Expected: 0xFF, Received: 0x{response.Header.CommandSet:X2}"); // server
            Utils.Assert(response.Header.CommandId == 0x00, $"Response must have OK command id. Expected: 0x00, Received: 0x{response.Header.CommandId:X2}"); // OK

            // Parse payload
            // uint64_t ProcessId;
            // GUID RuntimeCookie;
            // LPCWSTR CommandLine;
            // LPCWSTR OS;
            // LPCWSTR Arch;

            int totalSize = response.Payload.Length;
            Logger.logger.Log($"Total size of Payload = {totalSize} bytes");

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

            // ActiveIssue https://github.com/dotnet/runtime/issues/62729
            if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS())
            {
                // The following logic is tailored to this specific test where the cmdline _should_ look like the following:
                // /path/to/corerun /path/to/processinfo.dll
                // or
                // "C:\path\to\CoreRun.exe" C:\path\to\processinfo.dll
                string currentProcessCommandLine = TestLibrary.Utilities.IsSingleFile
                    ? currentProcess.MainModule.FileName
                    : $"{currentProcess.MainModule.FileName} {System.Reflection.Assembly.GetExecutingAssembly().Location}";
                string receivedCommandLine = NormalizeCommandLine(commandLine);
                Utils.Assert(currentProcessCommandLine.Equals(receivedCommandLine, StringComparison.OrdinalIgnoreCase), $"CommandLine must match current process. Expected: {currentProcessCommandLine}, Received: {receivedCommandLine} (original: {commandLine})");
            }

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
            else if (OperatingSystem.IsAndroid())
            {
                expectedOSValue = "Android";
            }
            else if (OperatingSystem.IsIOS())
            {
                expectedOSValue = "iOS";
            }
            else if (OperatingSystem.IsTvOS())
            {
                expectedOSValue = "tvOS";
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

            // VALIDATE ManagedEntrypointAssemblyName
            start = end;
            end = start + 4 /* sizeof(uint32_t) */;
            UInt32 managedEntrypointAssemblyNameLength = BitConverter.ToUInt32(response.Payload[start..end]);
            Logger.logger.Log($"managedEntrypointAssemblyNameLength: {managedEntrypointAssemblyNameLength}");

            start = end;
            end = start + ((int)managedEntrypointAssemblyNameLength * sizeof(char));
            Utils.Assert(end <= totalSize, $"String end can't exceed payload size. Expected: <{totalSize}, Received: {end} (decoded length: {managedEntrypointAssemblyNameLength})");
            Logger.logger.Log($"ManagedEntrypointAssemblyName bytes: [ {response.Payload[start..end].Select(b => b.ToString("X2") + " ").Aggregate(string.Concat)}]");
            string managedEntrypointAssemblyName = System.Text.Encoding.Unicode.GetString(response.Payload[start..end]).TrimEnd('\0');
            Logger.logger.Log($"ManagedEntrypointAssemblyName: \"{managedEntrypointAssemblyName}\"");

            string expectedManagedEntrypointAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            Utils.Assert(expectedManagedEntrypointAssemblyName.Equals(managedEntrypointAssemblyName), $"ManagedEntrypointAssemblyName must match. Expected: \"{expectedManagedEntrypointAssemblyName}\", received: \"{managedEntrypointAssemblyName}\"");

            // VALIDATE ClrProductVersion
            start = end;
            end = start + 4 /* sizeof(uint32_t) */;
            UInt32 clrProductVersionSize = BitConverter.ToUInt32(response.Payload[start..end]);
            Logger.logger.Log($"clrProductVersionSize: {clrProductVersionSize}");

            start = end;
            end = start + ((int)clrProductVersionSize * sizeof(char));
            Utils.Assert(end <= totalSize, $"String end can't exceed payload size. Expected: <{totalSize}, Received: {end} (decoded length: {clrProductVersionSize})");
            Logger.logger.Log($"ClrProductVersion bytes: [ {response.Payload[start..end].Select(b => b.ToString("X2") + " ").Aggregate(string.Concat)}]");
            string clrProductVersion = System.Text.Encoding.Unicode.GetString(response.Payload[start..end]).TrimEnd('\0');
            Logger.logger.Log($"ClrProductVersion: \"{clrProductVersion}\"");

            string expectedClrProductVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            Utils.Assert(expectedClrProductVersion.Equals(clrProductVersion), $"ClrProductVersion must match. Expected: \"{expectedClrProductVersion}\", received: \"{clrProductVersion}\"");

            Utils.Assert(end == totalSize, $"Full payload should have been read. Expected: {totalSize}, Received: {end}");

            Logger.logger.Log($"\n{{\n\tprocessId: {processId},\n\truntimeCookie: {runtimeCookie},\n\tcommandLine: {commandLine},\n\tOS: {OS},\n\tArch: {arch},\n\tManagedEntrypointAssemblyName: {managedEntrypointAssemblyName},\n\tClrProductVersion: {clrProductVersion}\n}}");
        }
    }
}