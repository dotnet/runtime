// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

        public enum Toolhelp32Flags : uint
        {
            TH32CS_INHERIT = 0x80000000,
            TH32CS_SNAPHEAPLIST = 0x00000001,
            TH32CS_SNAPMODULE = 0x00000008,
            TH32CS_SNAPMODULE32 = 0x00000010,
            TH32CS_SNAPPROCESS = 0x00000002,
            TH32CS_SNAPTHREAD = 0x00000004
        };

        public unsafe struct ProcessEntry32
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
        public static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);
    }

    public class CoreclrTestWrapperLib
    {
        public const int EXIT_SUCCESS_CODE = 0;
        public const string TIMEOUT_ENVIRONMENT_VAR = "__TestTimeout";
        
        // Default timeout set to 10 minutes
        public const int DEFAULT_TIMEOUT = 1000 * 60*10;

        public const string COLLECT_DUMPS_ENVIRONMENT_VAR = "__CollectDumps";
        public const string CRASH_DUMP_FOLDER_ENVIRONMENT_VAR = "__CrashDumpFolder";

        static bool CollectCrashDump(Process process, string path)
        {
            using (var crashDump = File.OpenWrite(path))
            {
                var flags = DbgHelp.MiniDumpType.MiniDumpWithFullMemory | DbgHelp.MiniDumpType.MiniDumpIgnoreInaccessibleMemory;
                return DbgHelp.MiniDumpWriteDump(process.Handle, process.Id, crashDump.SafeFileHandle, flags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
        }

        static unsafe bool TryFindChildProcessByName(Process process, string childName, out Process child)
        {
            IntPtr snapshot = Kernel32.CreateToolhelp32Snapshot(Kernel32.Toolhelp32Flags.TH32CS_SNAPPROCESS, 0);
            if (snapshot == IntPtr.Zero)
            {
                child = null;
                return false;
            }

            try
            {
                int ppid = process.Id;

                var processEntry = new Kernel32.ProcessEntry32 { Size = sizeof(Kernel32.ProcessEntry32) };

                bool success = Kernel32.Process32First(snapshot, ref processEntry);
                while (success)
                {
                    if (processEntry.ParentProcessID == ppid)
                    {
                        try
                        {
                            Process c = Process.GetProcessById(processEntry.ProcessID);
                            if (c.ProcessName.Equals(childName, StringComparison.OrdinalIgnoreCase))
                            {
                                child = c;
                                return true;
                            }
                            c.Dispose();
                        }
                        catch {}
                    }

                    success = Kernel32.Process32Next(snapshot, ref processEntry);
                }

                child = null;
                return false;
            }
            finally
            {
                Kernel32.CloseHandle(snapshot);
            }
        }

        public int RunTest(string executable, string outputFile, string errorFile)
        {
            Debug.Assert(outputFile != errorFile);

            int exitCode = -100;
            
            // If a timeout was given to us by an environment variable, use it instead of the default
            // timeout.
            string environmentVar = Environment.GetEnvironmentVariable(TIMEOUT_ENVIRONMENT_VAR);
            int timeout = environmentVar != null ? int.Parse(environmentVar) : DEFAULT_TIMEOUT;

            // Check if we are running in Windows
            string operatingSystem = System.Environment.GetEnvironmentVariable("OS");
            bool runningInWindows = (operatingSystem != null && operatingSystem.StartsWith("Windows"));

            // We can't yet take crash dumps on non-Windows OSs for timed-out tests
            bool collectCrashDumps = runningInWindows && Environment.GetEnvironmentVariable(COLLECT_DUMPS_ENVIRONMENT_VAR) != null;

            var outputStream = new FileStream(outputFile, FileMode.Create);
            var errorStream = new FileStream(errorFile, FileMode.Create);

            using (var outputWriter = new StreamWriter(outputStream))
            using (var errorWriter = new StreamWriter(errorStream))
            using (Process process = new Process())
            {
                // Windows can run the executable implicitly
                if (runningInWindows)
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
                    try
                    {
                        cts.Cancel();
                    }
                    catch {}

                    outputWriter.WriteLine("\ncmdLine:" + executable + " Timed Out");
                    errorWriter.WriteLine("\ncmdLine:" + executable + " Timed Out");

                    if (collectCrashDumps)
                    {
                        string crashDumpFolder = Environment.GetEnvironmentVariable(CRASH_DUMP_FOLDER_ENVIRONMENT_VAR);
                        if (crashDumpFolder != null)
                        {
                            Process childProcess;
                            if (TryFindChildProcessByName(process, "corerun", out childProcess))
                            {
                                string crashDumpPath = Path.Combine(Path.GetFullPath(crashDumpFolder), string.Format("crashdump_{0}.dmp", childProcess.Id));
                                if (CollectCrashDump(childProcess, crashDumpPath))
                                {
                                    Console.WriteLine("Collected crash dump {0}", crashDumpPath);
                                }
                            }
                        }
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
