// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace TestLibrary
{
    static class DbgHelp
    {
        public enum MiniDumpType : int
        {
            MiniDumpNormal                          = 0x00000000,
            MiniDumpWithDataSegs                    = 0x00000001,
            MiniDumpWithFullMemory                  = 0x00000002,
            MiniDumpWithHandleData                  = 0x00000004,
            MiniDumpFilterMemory                    = 0x00000008,
            MiniDumpScanMemory                      = 0x00000010,
            MiniDumpWithUnloadedModules             = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory  = 0x00000040,
            MiniDumpFilterModulePaths               = 0x00000080,
            MiniDumpWithProcessThreadData           = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory      = 0x00000200,
            MiniDumpWithoutOptionalData             = 0x00000400,
            MiniDumpWithFullMemoryInfo              = 0x00000800,
            MiniDumpWithThreadInfo                  = 0x00001000,
            MiniDumpWithCodeSegs                    = 0x00002000,
            MiniDumpWithoutAuxiliaryState           = 0x00004000,
            MiniDumpWithFullAuxiliaryState          = 0x00008000,
            MiniDumpWithPrivateWriteCopyMemory      = 0x00010000,
            MiniDumpIgnoreInaccessibleMemory        = 0x00020000,
            MiniDumpWithTokenInformation            = 0x00040000,
            MiniDumpWithModuleHeaders               = 0x00080000,
            MiniDumpFilterTriage                    = 0x00100000,
            MiniDumpValidTypeFlags                  = 0x001fffff
        }

        [DllImport("DbgHelp.dll", SetLastError = true)]
        public static extern bool MiniDumpWriteDump(IntPtr handle, int processId, SafeFileHandle file, MiniDumpType dumpType, IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);
    }

    static class Kernel32
    {
        public const int MAX_PATH = 260;
        public const int ERROR_NO_MORE_FILES = 0x12;
        public const long INVALID_HANDLE = -1;

        public enum Toolhelp32Flags : uint
        {
            TH32CS_INHERIT = 0x80000000,
            TH32CS_SNAPHEAPLIST = 0x00000001,
            TH32CS_SNAPMODULE = 0x00000008,
            TH32CS_SNAPMODULE32 = 0x00000010,
            TH32CS_SNAPPROCESS = 0x00000002,
            TH32CS_SNAPTHREAD = 0x00000004
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe struct ProcessEntry32W
        {
            public int Size;
            public int Usage;
            public int ProcessID;
            public IntPtr DefaultHeapID;
            public int ModuleID;
            public int Threads;
            public int ParentProcessID;
            public int PriClassBase;
            public int Flags;
            public fixed char ExeFile[MAX_PATH];
        }

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(Toolhelp32Flags flags, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool Process32FirstW(IntPtr snapshot, ref ProcessEntry32W entry);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool Process32NextW(IntPtr snapshot, ref ProcessEntry32W entry);
    }

    static class @libproc
    {
        [DllImport(nameof(libproc))]
        private static extern int proc_listchildpids(int ppid, int[]? buffer, int byteSize);

        public static unsafe bool ListChildPids(int ppid, out int[] buffer)
        {
            int n = proc_listchildpids(ppid, null, 0);
            buffer = new int[n];
            return proc_listchildpids(ppid, buffer, buffer.Length * sizeof(int)) != -1;
        }
    }

    internal static class ProcessExtensions
    {
        public static bool TryGetProcessId(this Process process, out int processId)
        {
            try
            {
                processId = process.Id;
                return true;
            }
            catch
            {
                // Process exited
                processId = default;
                return false;
            }
        }

        public static bool TryGetProcessName(this Process process, out string processName)
        {
            try
            {
                processName = process.ProcessName;
                return true;
            }
            catch
            {
                // Process exited
                processName = default;
                return false;
            }
        }

        public unsafe static IEnumerable<Process> GetChildren(this Process process)
        {
            var children = new List<Process>();
            if (OperatingSystem.IsWindows())
            {
                return Windows_GetChildren(process);
            }
            else if (OperatingSystem.IsLinux())
            {
                return Linux_GetChildren(process);
            }
            else if (OperatingSystem.IsMacOS())
            {
                return MacOS_GetChildren(process);
            }
            return children;
        }

        private unsafe static IEnumerable<Process> Windows_GetChildren(Process process)
        {
            var children = new List<Process>();
            IntPtr snapshot = Kernel32.CreateToolhelp32Snapshot(Kernel32.Toolhelp32Flags.TH32CS_SNAPPROCESS, 0);
            if (snapshot != IntPtr.Zero && snapshot.ToInt64() != Kernel32.INVALID_HANDLE)
            {
                try
                {
                    children = new List<Process>();
                    int ppid = process.Id;

                    var processEntry = new Kernel32.ProcessEntry32W { Size = sizeof(Kernel32.ProcessEntry32W) };

                    bool success = Kernel32.Process32FirstW(snapshot, ref processEntry);
                    while (success)
                    {
                        if (processEntry.ParentProcessID == ppid)
                        {
                            try
                            {
                                children.Add(Process.GetProcessById(processEntry.ProcessID));
                            }
                            catch {}
                        }

                        success = Kernel32.Process32NextW(snapshot, ref processEntry);
                    }

                }
                finally
                {
                    Kernel32.CloseHandle(snapshot);
                }
            }

            return children;
        }

        private static IEnumerable<Process> Linux_GetChildren(Process process)
        {
            var children = new List<Process>();
            List<int>? childPids = null;

            try
            {
                childPids = File.ReadAllText($"/proc/{process.Id}/task/{process.Id}/children")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(pidString => int.Parse(pidString))
                    .ToList();
            }
            catch (IOException e)
            {
                // Some distros might not have the /proc/pid/task/tid/children entry enabled in the kernel
                // attempt to use pgrep then
                var pgrepInfo = new ProcessStartInfo("pgrep");
                pgrepInfo.RedirectStandardOutput = true;
                pgrepInfo.Arguments = $"-P {process.Id}";

                using Process pgrep = Process.Start(pgrepInfo)!;

                string[] pidStrings = pgrep.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                pgrep.WaitForExit();

                childPids = new List<int>();
                foreach (var pidString in pidStrings)
                    if (int.TryParse(pidString, out int childPid))
                        childPids.Add(childPid);
            }

            foreach (var pid in childPids)
            {
                try
                {
                    children.Add(Process.GetProcessById(pid));
                }
                catch (ArgumentException)
                {
                    // Ignore failure to get process, the process may have exited
                }
            }

            return children;
        }

        private static IEnumerable<Process> MacOS_GetChildren(Process process)
        {
            var children = new List<Process>();
            if (libproc.ListChildPids(process.Id, out int[] childPids))
            {
                foreach (var childPid in childPids)
                {
                    children.Add(Process.GetProcessById(childPid));
                }
            }

            return children;
        }
    }

    internal class CoreclrTestWrapperLib
    {
        public const int EXIT_SUCCESS_CODE = 0;
        public const string TIMEOUT_ENVIRONMENT_VAR = "__TestTimeout";

        // Default timeout set to 10 minutes
        public const int DEFAULT_TIMEOUT_MS = 1000 * 60 * 10;

        public const string COLLECT_DUMPS_ENVIRONMENT_VAR = "__CollectDumps";
        public const string CRASH_DUMP_FOLDER_ENVIRONMENT_VAR = "__CrashDumpFolder";

        public const string TEST_TARGET_ARCHITECTURE_ENVIRONMENT_VAR = "__TestArchitecture";

        static bool CollectCrashDump(Process process, string crashDumpPath, StreamWriter outputWriter)
        {
            if (OperatingSystem.IsWindows())
            {
                return CollectCrashDumpWithMiniDumpWriteDump(process, crashDumpPath, outputWriter);
            }
            else
            {
                return CollectCrashDumpWithCreateDump(process, crashDumpPath, outputWriter);
            }
        }

        static bool CollectCrashDumpWithMiniDumpWriteDump(Process process, string crashDumpPath, StreamWriter outputWriter)
        {
            bool collectedDump = false;
            using (var crashDump = File.OpenWrite(crashDumpPath))
            {
                var flags = DbgHelp.MiniDumpType.MiniDumpWithFullMemory | DbgHelp.MiniDumpType.MiniDumpIgnoreInaccessibleMemory;
                collectedDump = DbgHelp.MiniDumpWriteDump(process.Handle, process.Id, crashDump.SafeFileHandle, flags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
            return collectedDump;
        }

        static bool CollectCrashDumpWithCreateDump(Process process, string crashDumpPath, StreamWriter outputWriter)
        {
            string? coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
            if (coreRoot is null)
            {
                throw new InvalidOperationException("CORE_ROOT environment variable is not set.");
            }
            string createdumpPath = Path.Combine(coreRoot, "createdump");
            string arguments = $"--crashreport --name \"{crashDumpPath}\" {process.Id} --withheap";
            Process createdump = new Process();

            createdump.StartInfo.FileName = "sudo";
            createdump.StartInfo.Arguments = $"{createdumpPath} {arguments}";

            createdump.StartInfo.UseShellExecute = false;
            createdump.StartInfo.RedirectStandardOutput = true;
            createdump.StartInfo.RedirectStandardError = true;

            Console.WriteLine($"Invoking: {createdump.StartInfo.FileName} {createdump.StartInfo.Arguments}");
            createdump.Start();

            Task<string> copyOutput = createdump.StandardOutput.ReadToEndAsync();
            Task<string> copyError = createdump.StandardError.ReadToEndAsync();
            bool fSuccess = createdump.WaitForExit(DEFAULT_TIMEOUT_MS);

            if (fSuccess)
            {
                Task.WaitAll(copyError, copyOutput);
                string output = copyOutput.Result;
                string error = copyError.Result;

                Console.WriteLine("createdump stdout:");
                Console.WriteLine(output);
                Console.WriteLine("createdump stderr:");
                Console.WriteLine(error);

                // Ensure the dump is accessible by current user
                Process chown = new Process();
                chown.StartInfo.FileName = "sudo";
                chown.StartInfo.Arguments = $"chown \"{Environment.UserName}\" \"{crashDumpPath}\"";

                chown.StartInfo.UseShellExecute = false;
                chown.StartInfo.RedirectStandardOutput = true;
                chown.StartInfo.RedirectStandardError = true;

                Console.WriteLine($"Invoking: {chown.StartInfo.FileName} {chown.StartInfo.Arguments}");
                chown.Start();
                copyOutput = chown.StandardOutput.ReadToEndAsync();
                copyError = chown.StandardError.ReadToEndAsync();

                chown.WaitForExit(DEFAULT_TIMEOUT_MS);

                Task.WaitAll(copyError, copyOutput);
                Console.WriteLine("chown stdout:");
                Console.WriteLine(copyOutput.Result);
                Console.WriteLine("chown stderr:");
                Console.WriteLine(copyError.Result);
            }
            else
            {
                // Workaround for https://github.com/dotnet/runtime/issues/93321
                const int MaxRetries = 5;
                for (int i = 0; i < MaxRetries; i++)
                {
                    try
                    {
                        createdump.Kill(entireProcessTree: true);
                        break;
                    }
                    catch (Exception e) when (i < MaxRetries - 1)
                    {
                        Console.WriteLine($"Process.Kill(entireProcessTree: true) failed:");
                        Console.WriteLine(e);
                        Console.WriteLine("Retrying...");
                    }
                }
            }

            return fSuccess && createdump.ExitCode == 0;
        }

        // Finds all children processes starting with a process named childName
        // The children are sorted in the order they should be dumped
        static unsafe IEnumerable<Process> FindChildProcessesByName(Process process, string childName)
        {
            process.TryGetProcessName(out string parentProcessName);
            process.TryGetProcessId(out int parentProcessId);
            Console.WriteLine($"Finding all child processes of '{parentProcessName}' (ID: {parentProcessId}) with name '{childName}'");

            var children = new Stack<Process>();
            Queue<Process> childrenToCheck = new Queue<Process>();
            HashSet<int> seen = new HashSet<int>();

            seen.Add(parentProcessId);

            try
            {
                foreach (var child in process.GetChildren())
                    childrenToCheck.Enqueue(child);
            }
            catch
            {
                // Process exited
            }

            while (childrenToCheck.Count != 0)
            {
                Process child = childrenToCheck.Dequeue();

                if (!child.TryGetProcessId(out int processId))
                    continue;

                if (seen.Contains(processId))
                    continue;

                if (!child.TryGetProcessName(out string processName))
                    continue;

                Console.WriteLine($"Checking child process: '{processName}' (ID: {processId})");
                seen.Add(processId);

                try
                {
                    foreach (var grandchild in child.GetChildren())
                        childrenToCheck.Enqueue(grandchild);
                }
                catch
                {
                    // Process exited
                }

                if (processName.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    children.Push(child);
                }
            }

            return children;
        }

        public int RunTest(string executable, string outputFile, string errorFile, string category, string testBinaryBase, string outputDir)
        {
            Debug.Assert(outputFile != errorFile);

            int exitCode = -100;

            // If a timeout was given to us by an environment variable, use it instead of the default
            // timeout.
            string? environmentVar = Environment.GetEnvironmentVariable(TIMEOUT_ENVIRONMENT_VAR);
            int timeout = environmentVar != null ? int.Parse(environmentVar) : DEFAULT_TIMEOUT_MS;
            bool collectCrashDumps = Environment.GetEnvironmentVariable(COLLECT_DUMPS_ENVIRONMENT_VAR) != null;
            string? crashDumpFolder = Environment.GetEnvironmentVariable(CRASH_DUMP_FOLDER_ENVIRONMENT_VAR);

            var outputStream = new FileStream(outputFile, FileMode.Create);
            var errorStream = new FileStream(errorFile, FileMode.Create);

            using (var outputWriter = new StreamWriter(outputStream))
            using (var errorWriter = new StreamWriter(errorStream))
            using (Process process = new Process())
            {
                // Windows can run the executable implicitly
                if (OperatingSystem.IsWindows())
                {
                    process.StartInfo.FileName = executable;
                }
                // Non-windows needs to be told explicitly to run through /bin/bash shell
                else
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = executable;
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.EnvironmentVariables.Add("__Category", category);
                process.StartInfo.EnvironmentVariables.Add("__TestBinaryBase", testBinaryBase);
                process.StartInfo.EnvironmentVariables.Add("__OutputDir", outputDir);

                DateTime startTime = DateTime.Now;
                process.Start();

                var cts = new CancellationTokenSource();
                Task copyOutput = process.StandardOutput.BaseStream.CopyToAsync(outputStream, 4096, cts.Token);
                Task copyError = process.StandardError.BaseStream.CopyToAsync(errorStream, 4096, cts.Token);

                if (process.WaitForExit(timeout))
                {
                    // Process completed. Check process.ExitCode here.
                    exitCode = process.ExitCode;
                    Task.WaitAll(copyOutput, copyError);
                }
                else
                {
                    // Timed out.
                    DateTime endTime = DateTime.Now;

                    try
                    {
                        cts.Cancel();
                    }
                    catch { }

                    outputWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}{2}{3}, start: {4}, end: {5})",
                            executable, timeout, (environmentVar != null) ? " from variable " : "", (environmentVar != null) ? TIMEOUT_ENVIRONMENT_VAR : "",
                            startTime.ToString(), endTime.ToString());
                    outputWriter.Flush();
                    errorWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}{2}{3}, start: {4}, end: {5})",
                            executable, timeout, (environmentVar != null) ? " from variable " : "", (environmentVar != null) ? TIMEOUT_ENVIRONMENT_VAR : "",
                            startTime.ToString(), endTime.ToString());
                    errorWriter.Flush();

                    Console.WriteLine("Collecting diagnostic information...");
                    Console.WriteLine("Snapshot of processes currently running:");
                    Console.WriteLine($"\t{"ID",-6} ProcessName");
                    foreach (var activeProcess in Process.GetProcesses())
                    {
                        activeProcess.TryGetProcessName(out string activeProcessName);
                        activeProcess.TryGetProcessId(out int activeProcessId);
                        Console.WriteLine($"\t{activeProcessId,-6} {activeProcessName}");
                    }

                    if (OperatingSystem.IsWindows())
                    {
                        Console.WriteLine("Snapshot of processes currently running (using wmic):");
                        Console.WriteLine(GetAllProcessNames_wmic());
                    }

                    if (collectCrashDumps)
                    {
                        if (crashDumpFolder != null)
                        {
                            foreach (var child in FindChildProcessesByName(process, "corerun"))
                            {
                                string crashDumpPath = Path.Combine(Path.GetFullPath(crashDumpFolder), string.Format("crashdump_{0}.dmp", child.Id));
                                Console.WriteLine($"Attempting to collect crash dump: {crashDumpPath}");
                                if (CollectCrashDump(child, crashDumpPath, outputWriter))
                                {
                                    Console.WriteLine("Collected crash dump: {0}", crashDumpPath);
                                }
                                else
                                {
                                    Console.WriteLine("Failed to collect crash dump");
                                }
                            }
                        }
                    }

                    // kill the timed out processes after we've collected dumps
                    process.Kill(entireProcessTree: true);
                }

                outputWriter.WriteLine("Test Harness Exitcode is : " + exitCode.ToString());
                outputWriter.Flush();
                errorWriter.Flush();
            }

            return exitCode;
        }

        private static string GetAllProcessNames_wmic()
        {
            // The command to execute
            string command = "wmic process get Name, ProcessId, ParentProcessId";

            // Start the process and capture the output
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            // Start the process and read the output
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(100); // wait for 100 ms

            // Output the result
            return output;
        }
    }
}
