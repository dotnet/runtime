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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace CoreclrTestLib
{
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

    static class libproc
    {
        [DllImport(nameof(libproc))]
        private static extern int proc_listchildpids(int ppid, int[] buffer, int byteSize);

        public static unsafe bool ListChildPids(int ppid, out int[] buffer)
        {
            int n = proc_listchildpids(ppid, null, 0);
            buffer = new int[n];
            return proc_listchildpids(ppid, buffer, buffer.Length * sizeof(int)) != -1;
        }
    }

    internal static class ProcessExtensions
    {
        public unsafe static IEnumerable<Process> GetChildren(this Process process)
        {
            var children = new List<Process>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Windows_GetChildren(process);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Linux_GetChildren(process);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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
            List<int> childPids = null;

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

                using Process pgrep = Process.Start(pgrepInfo);

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

    public class CoreclrTestWrapperLib
    {
        public const int EXIT_SUCCESS_CODE = 0;
        public const string TIMEOUT_ENVIRONMENT_VAR = "__TestTimeout";
        
        // Default timeout set to 10 minutes
        public const int DEFAULT_TIMEOUT_MS = 1000 * 60 * 10;

        public const string COLLECT_DUMPS_ENVIRONMENT_VAR = "__CollectDumps";
        public const string CRASH_DUMP_FOLDER_ENVIRONMENT_VAR = "__CrashDumpFolder";

        static bool CollectCrashDump(Process process, string path)
        {
            ProcessStartInfo createdumpInfo = null;
            string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
            string createdumpPath = Path.Combine(coreRoot, "createdump");
            string arguments = $"--name \"{path}\" {process.Id} --withheap";
            Process createdump = new Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                createdump.StartInfo.FileName = createdumpPath + ".exe";
                createdump.StartInfo.Arguments = arguments;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                createdump.StartInfo.FileName = "sudo";
                createdump.StartInfo.Arguments = $"{createdumpPath} " + arguments;
            }

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
            }
            else
            {
                createdump.Kill(true);
            }

            return fSuccess && createdump.ExitCode == 0;
        }

        // Finds all children processes starting with a process named childName
        // The children are sorted in the order they should be dumped
        static unsafe IEnumerable<Process> FindChildProcessesByName(Process process, string childName)
        {
            var children = new Stack<Process>();
            Queue<Process> childrenToCheck = new Queue<Process>();
            HashSet<int> seen = new HashSet<int>();

            seen.Add(process.Id);
            foreach (var child in process.GetChildren())
                childrenToCheck.Enqueue(child);

            while (childrenToCheck.Count != 0)
            {
                Process child = childrenToCheck.Dequeue();
                if (seen.Contains(child.Id))
                    continue;

                seen.Add(child.Id);

                foreach (var grandchild in child.GetChildren())
                    childrenToCheck.Enqueue(grandchild);

                if (child.ProcessName.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    children.Push(child);
                }
            }

            return children;
        }

        public int RunTest(string executable, string outputFile, string errorFile)
        {
            Debug.Assert(outputFile != errorFile);

            int exitCode = -100;
            
            // If a timeout was given to us by an environment variable, use it instead of the default
            // timeout.
            string environmentVar = Environment.GetEnvironmentVariable(TIMEOUT_ENVIRONMENT_VAR);
            int timeout = environmentVar != null ? int.Parse(environmentVar) : DEFAULT_TIMEOUT_MS;
            bool collectCrashDumps = Environment.GetEnvironmentVariable(COLLECT_DUMPS_ENVIRONMENT_VAR) != null;
            string crashDumpFolder = Environment.GetEnvironmentVariable(CRASH_DUMP_FOLDER_ENVIRONMENT_VAR);

            var outputStream = new FileStream(outputFile, FileMode.Create);
            var errorStream = new FileStream(errorFile, FileMode.Create);

            using (var outputWriter = new StreamWriter(outputStream))
            using (var errorWriter = new StreamWriter(errorStream))
            using (Process process = new Process())
            {
                // Windows can run the executable implicitly
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                    catch {}

                    outputWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}{2}{3}, start: {4}, end: {5})",
                            executable, timeout, (environmentVar != null) ? " from variable " : "", (environmentVar != null) ? TIMEOUT_ENVIRONMENT_VAR : "",
                            startTime.ToString(), endTime.ToString());
                    errorWriter.WriteLine("\ncmdLine:{0} Timed Out (timeout in milliseconds: {1}{2}{3}, start: {4}, end: {5})",
                            executable, timeout, (environmentVar != null) ? " from variable " : "", (environmentVar != null) ? TIMEOUT_ENVIRONMENT_VAR : "",
                            startTime.ToString(), endTime.ToString());

                    if (collectCrashDumps)
                    {
                        if (crashDumpFolder != null)
                        {
                            foreach (var child in FindChildProcessesByName(process, "corerun"))
                            {
                                string crashDumpPath = Path.Combine(Path.GetFullPath(crashDumpFolder), string.Format("crashdump_{0}.dmp", child.Id));
                                Console.WriteLine($"Attempting to collect crash dump: {crashDumpPath}");
                                if (CollectCrashDump(child, crashDumpPath))
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

        
    }
}
