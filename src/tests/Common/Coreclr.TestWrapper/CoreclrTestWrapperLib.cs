// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#nullable disable

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

namespace CoreclrTestLib
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
            if (collectedDump)
            {
                TryPrintStackTraceFromDmp(crashDumpPath, outputWriter);
            }
            return collectedDump;
        }

        static bool CollectCrashDumpWithCreateDump(Process process, string crashDumpPath, StreamWriter outputWriter)
        {
            string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
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

                TryPrintStackTraceFromCrashReport(crashDumpPath + ".crashreport.json", outputWriter);
            }
            else
            {
                createdump.Kill(true);
            }

            return fSuccess && createdump.ExitCode == 0;
        }

        private static List<string> knownNativeModules = new List<string>() { "libcoreclr.so", "libclrjit.so" };
        private static string TO_BE_CONTINUE_TAG = "<TO_BE_CONTINUE>";
        private static string SKIP_LINE_TAG = "# <SKIP_LINE>";


        static bool RunProcess(string fileName, string arguments, TextWriter outputWriter)
        {
            Process proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            outputWriter.WriteLine($"Invoking: {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
            proc.Start();

            Task<string> stdOut = proc.StandardOutput.ReadToEndAsync();
            Task<string> stdErr = proc.StandardError.ReadToEndAsync();
            if(!proc.WaitForExit(DEFAULT_TIMEOUT_MS))
            {
                proc.Kill(true);
                outputWriter.WriteLine($"Timedout: '{fileName} {arguments}");
                return false;
            }

            Task.WaitAll(stdOut, stdErr);
            string output = stdOut.Result;
            string error = stdErr.Result;
            if (!string.IsNullOrWhiteSpace(output))
            {
                outputWriter.WriteLine($"stdout: {output}");
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                outputWriter.WriteLine($"stderr: {error}");
            }
            return true;
        }

        /// <summary>
        ///     Parse crashreport.json file, use llvm-symbolizer to extract symbols
        ///     and recreate the stacktrace that is printed on the console.
        /// </summary>
        /// <param name="crashReportJsonFile">crash dump path</param>
        /// <param name="outputWriter">Stream for writing logs</param>
        /// <returns>true, if we can print the stack trace, otherwise false.</returns>
        public static bool TryPrintStackTraceFromCrashReport(string crashReportJsonFile, TextWriter outputWriter)
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (!RunProcess("sudo", $"ls -l {crashReportJsonFile}", Console.Out))
                {
                    return false;
                }

                Console.WriteLine("=========================================");
                string userName = Environment.GetEnvironmentVariable("USER");
                if (!string.IsNullOrEmpty(userName))
                {
                    if (!RunProcess("sudo", $"chown {userName} {crashReportJsonFile}", Console.Out))
                    {
                        return false;
                    }

                    Console.WriteLine("=========================================");
                    if (!RunProcess("sudo", $"ls -l {crashReportJsonFile}", Console.Out))
                    {
                        return false;
                    }

                    Console.WriteLine("=========================================");
                    if (!RunProcess("ls", $"-l {crashReportJsonFile}", Console.Out))
                    {
                        return false;
                    }
                }
            }

            if (!File.Exists(crashReportJsonFile))
            {
                return false;
            }
            outputWriter.WriteLine($"Printing stacktrace from '{crashReportJsonFile}'");

            string contents;
            try
            {
                contents = File.ReadAllText(crashReportJsonFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {crashReportJsonFile}: {ex.ToString()}");
                return false;
            }
            dynamic crashReport = JsonSerializer.Deserialize<JsonObject>(contents);
            var threads = crashReport["payload"]["threads"];

            // The logic happens in 3 steps:
            // 1. Read the crashReport.json file, locate all the addresses of interest and then build
            //    a string that will be passed to llvm-symbolizer. It is populated so that each address
            //    is in its separate line along with the file name, etc. Some TAGS are added in the
            //    string that is used in step 2.
            // 2. llvm-symbolizer is ran and above string is passed as input.
            // 3. After llvm-symbolizer completes, TAGS are used to format its output to print it in
            //    the way it will be printed by sos.

            StringBuilder addrBuilder = new StringBuilder();
            string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT") ?? string.Empty;
            foreach (var thread in threads)
            {

                if (thread["native_thread_id"] == null)
                {
                    continue;
                }

                addrBuilder.AppendLine();
                addrBuilder.AppendLine("----------------------------------");
                addrBuilder.AppendLine($"Thread Id: {thread["native_thread_id"]}");
                addrBuilder.AppendLine("      Child SP               IP Call Site");
                var stack_frames = thread["stack_frames"];
                foreach (var frame in stack_frames)
                {
                    addrBuilder.Append($"{SKIP_LINE_TAG} {frame["stack_pointer"]} {frame["native_address"]} ");
                    bool isNative = (string)frame["is_managed"] == "false";

                    if (isNative)
                    {
                        string nativeModuleName = (string)frame["native_module"];
                        string unmanagedName = (string)frame["unmanaged_name"];

                        if ((nativeModuleName != null) && (knownNativeModules.Contains(nativeModuleName)))
                        {
                            // Need to use llvm-symbolizer (only if module_address != 0)
                            AppendAddress(addrBuilder, coreRoot, nativeModuleName, (string)frame["native_address"], (string)frame["module_address"]);
                        }
                        else if ((nativeModuleName != null) || (unmanagedName != null))
                        {
                            if (nativeModuleName != null)
                            {
                                addrBuilder.Append($"{nativeModuleName}!");
                            }
                            if (unmanagedName != null)
                            {
                                addrBuilder.Append($"{unmanagedName}");
                            }
                        }
                    }
                    else
                    {
                        string fileName = (string)frame["filename"];
                        string methodName = (string)frame["method_name"];

                        if ((fileName != null) || (methodName != null))
                        {
                            // found the managed method name
                            if (fileName != null)
                            {
                                addrBuilder.Append($"{fileName}!");
                            }
                            if (methodName != null)
                            {
                                addrBuilder.Append($"{methodName}");
                            }
                        }
                        else
                        {
                            addrBuilder.Append($"{frame["native_address"]}");
                        }
                    }
                    addrBuilder.AppendLine();

                }
            }

            string symbolizerOutput = null;

            Process llvmSymbolizer = new Process()
            {
                StartInfo = {
                    FileName = "llvm-symbolizer",
                    Arguments = $"--pretty-print",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                }
            };

            outputWriter.WriteLine($"Invoking {llvmSymbolizer.StartInfo.FileName} {llvmSymbolizer.StartInfo.Arguments}");

            try
            {
                if (!llvmSymbolizer.Start())
                {
                    outputWriter.WriteLine($"Unable to start {llvmSymbolizer.StartInfo.FileName}");
                }

                using (var symbolizerWriter = llvmSymbolizer.StandardInput)
                {
                    symbolizerWriter.WriteLine(addrBuilder.ToString());
                }

                Task<string> stdout = llvmSymbolizer.StandardOutput.ReadToEndAsync();
                Task<string> stderr = llvmSymbolizer.StandardError.ReadToEndAsync();
                bool fSuccess = llvmSymbolizer.WaitForExit(DEFAULT_TIMEOUT_MS);

                Task.WaitAll(stdout, stderr);

                if (!fSuccess)
                {
                    outputWriter.WriteLine("Errors while running llvm-symbolizer --pretty-print");
                    string output = stdout.Result;
                    string error = stderr.Result;

                    Console.WriteLine("llvm-symbolizer stdout:");
                    Console.WriteLine(output);
                    Console.WriteLine("llvm-symbolizer stderr:");
                    Console.WriteLine(error);

                    llvmSymbolizer.Kill(true);

                    return false;
                }

                symbolizerOutput = stdout.Result;

            } catch (Exception e) {
                outputWriter.WriteLine("Errors while running llvm-symbolizer --pretty-print");
                outputWriter.WriteLine(e.ToString());
                return false;
            }

            // Go through the output of llvm-symbolizer and strip all the markers we added initially.
            string[] contentsToSantize = symbolizerOutput.Split(Environment.NewLine);
            StringBuilder finalBuilder = new StringBuilder();
            for (int lineNum = 0; lineNum < contentsToSantize.Length; lineNum++)
            {
                string line = contentsToSantize[lineNum].Replace(SKIP_LINE_TAG, string.Empty);
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.EndsWith(TO_BE_CONTINUE_TAG))
                {
                    finalBuilder.Append(line.Replace(TO_BE_CONTINUE_TAG, string.Empty));
                    continue;
                }
                finalBuilder.AppendLine(line);
            }
            outputWriter.WriteLine("Stack trace:");
            outputWriter.WriteLine(finalBuilder.ToString());
            return true;
        }

        private static void AppendAddress(StringBuilder sb, string coreRoot, string nativeModuleName, string native_address, string module_address)
        {
            if (module_address != "0x0")
            {
                sb.Append($"{nativeModuleName}!");
                sb.Append(TO_BE_CONTINUE_TAG);
                sb.AppendLine();
                //addrBuilder.AppendLine(frame.native_image_offset);
                ulong nativeAddress = ulong.Parse(native_address.Substring(2), System.Globalization.NumberStyles.HexNumber);
                ulong moduleAddress = ulong.Parse(module_address.Substring(2), System.Globalization.NumberStyles.HexNumber);
                string fullPathToModule = Path.Combine(coreRoot, nativeModuleName);
                sb.AppendFormat("{0} 0x{1:x}", fullPathToModule, nativeAddress - moduleAddress);
            }
        }

        public static bool TryPrintStackTraceFromDmp(string dmpFile, TextWriter outputWriter)
        {
            string targetArchitecture = Environment.GetEnvironmentVariable(TEST_TARGET_ARCHITECTURE_ENVIRONMENT_VAR);
            if (string.IsNullOrEmpty(targetArchitecture))
            {
                outputWriter.WriteLine($"Environment variable {TEST_TARGET_ARCHITECTURE_ENVIRONMENT_VAR} is not set.");
                return false;
            }

            string cdbPath = $@"C:\Program Files (x86)\Windows Kits\10\Debuggers\{targetArchitecture}\cdb.exe";
            if (!File.Exists(cdbPath))
            {
                outputWriter.WriteLine($"Unable to find cdb.exe at {cdbPath}");
                return false;
            }

            string sosPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "sos", "sos.dll");

            var cdbScriptPath = Path.GetTempFileName();
            File.WriteAllText(cdbScriptPath, $$"""
                .load {{sosPath}}
                ~*k
                !clrstack -f -all
                q
                """);

            // cdb outputs the stacks directly, so we don't need to parse the output.
            if (!RunProcess(cdbPath, $@"-c ""$<{cdbScriptPath}"" -z ""{dmpFile}""", outputWriter))
            {
                outputWriter.WriteLine("Unable to run cdb.exe");
                return false;
            }
            return true;
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

        public int RunTest(string executable, string outputFile, string errorFile, string category, string testBinaryBase, string outputDir)
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
                if (MobileAppHandler.IsRetryRequested(testBinaryBase))
                {
                    outputWriter.WriteLine("\nWork item retry had been requested earlier - skipping test...");
                }
                else
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
                        MobileAppHandler.CheckExitCode(exitCode, testBinaryBase, category, outputWriter);
                        Task.WaitAll(copyOutput, copyError);

                        if (exitCode != 0)
                        {
                            // Search for dump, if created.
                            if (Directory.Exists(crashDumpFolder))
                            {
                                outputWriter.WriteLine($"Test failed. Trying to see if dump file was created in {crashDumpFolder} since {startTime}");
                                DirectoryInfo crashDumpFolderInfo = new DirectoryInfo(crashDumpFolder);
                                // crashreport is only for non-windows.
                                if (!OperatingSystem.IsWindows())
                                {
                                    var dmpFilesInfo = crashDumpFolderInfo.GetFiles("*.crashreport.json").OrderByDescending(f => f.CreationTime);
                                    foreach (var dmpFile in dmpFilesInfo)
                                    {
                                        if (dmpFile.CreationTime < startTime)
                                        {
                                            // No new files since test started.
                                            outputWriter.WriteLine("Finish looking for *.crashreport.json. No new files created.");
                                            break;
                                        }
                                        outputWriter.WriteLine($"Processing {dmpFile.FullName}");
                                        TryPrintStackTraceFromCrashReport(dmpFile.FullName, outputWriter);
                                    }
                                }
                                else
                                {
                                    var dmpFilesInfo = crashDumpFolderInfo.GetFiles("*.dmp").OrderByDescending(f => f.CreationTime);
                                    foreach (var dmpFile in dmpFilesInfo)
                                    {
                                        if (dmpFile.CreationTime < startTime)
                                        {
                                            // No new files since test started.
                                            outputWriter.WriteLine("Finished looking for *.dmp. No new files created.");
                                            break;
                                        }
                                        outputWriter.WriteLine($"Processing {dmpFile.FullName}");
                                        TryPrintStackTraceFromDmp(dmpFile.FullName, outputWriter);
                                    }
                                }
                            }
                        }
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
                }

                outputWriter.WriteLine("Test Harness Exitcode is : " + exitCode.ToString());
                outputWriter.Flush();
                errorWriter.Flush();
            }

            return exitCode;
        }
    }
}
