// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// Tests the linked-in createdump functionality for NativeAOT on Linux.
/// When EventSourceSupport is enabled, NativeAOT links in a crash dump writer.
/// On crash, the process re-executes itself with a GUID sentinel to generate
/// an ELF core dump via ptrace.
///
/// Test flow:
///   Parent (no args)  → launches child with --crash
///   Child (--crash)   → crashes via null pointer dereference
///   NativeAOT runtime → detects crash, forks, re-execs self with sentinel
///   Re-exec'd process → writes ELF core dump to specified path
///   Parent            → verifies dump file exists and has valid ELF header
/// </summary>
class CreatedumpLinkedIn
{
    // ELF magic bytes: 0x7f 'E' 'L' 'F'
    static readonly byte[] ElfMagic = { 0x7f, 0x45, 0x4c, 0x46 };

    static unsafe int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--crash")
        {
            return CrashChild();
        }

        return RunParent();
    }

    static int RunParent()
    {
        string processPath = Environment.ProcessPath!;
        string dumpDir = Path.Combine(Path.GetTempPath(), "createdump_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dumpDir);
        string dumpPath = Path.Combine(dumpDir, "coredump.%p");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add("--crash");

            // Enable mini dump generation with a known output path.
            // DbgMiniDumpType=4 is DumpTypeFull.
            startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "1";
            startInfo.Environment["DOTNET_DbgMiniDumpName"] = dumpPath;
            startInfo.Environment["DOTNET_DbgMiniDumpType"] = "4";

            // Disable the system core_pattern so the OS doesn't also try to
            // write a core dump (which could interfere or be slow).
            startInfo.Environment["DOTNET_DbgDisableCorePattern"] = "1";

            Console.WriteLine($"Launching child process: {processPath} --crash");
            Console.WriteLine($"Dump path template: {dumpPath}");

            using Process child = Process.Start(startInfo)!;

            // Read stdout/stderr asynchronously to avoid deadlock.
            // ReadToEnd() blocks until the pipe closes; if we read one
            // stream synchronously, the child can block writing to the
            // other stream (full pipe buffer), causing a deadlock.
            var stdoutTask = child.StandardOutput.ReadToEndAsync();
            var stderrTask = child.StandardError.ReadToEndAsync();

            bool exited = child.WaitForExit(60_000);

            if (!exited)
            {
                Console.WriteLine("FAIL: Child process did not exit within timeout.");
                child.Kill();
                return 1;
            }

            string stdout = stdoutTask.GetAwaiter().GetResult();
            string stderr = stderrTask.GetAwaiter().GetResult();

            Console.WriteLine($"Child exit code: {child.ExitCode}");
            if (!string.IsNullOrEmpty(stdout))
                Console.WriteLine($"Child stdout: {stdout}");
            if (!string.IsNullOrEmpty(stderr))
                Console.WriteLine($"Child stderr: {stderr}");

            // The child should have been killed by a signal (exit code < 0 on .NET
            // for signal-terminated processes, or 128+signal on raw wait).
            // We don't check the exact exit code since it varies.

            // Look for the dump file. The %p in the template is replaced with the PID.
            string[] dumpFiles = Directory.GetFiles(dumpDir, "coredump.*");

            if (dumpFiles.Length == 0)
            {
                Console.WriteLine("FAIL: No dump file found in " + dumpDir);
                return 1;
            }

            Console.WriteLine($"Found {dumpFiles.Length} dump file(s):");
            foreach (string f in dumpFiles)
            {
                var info = new FileInfo(f);
                Console.WriteLine($"  {info.Name} ({info.Length} bytes)");
            }

            // Validate ELF header on the first dump file.
            string dumpFile = dumpFiles[0];
            var fileInfo = new FileInfo(dumpFile);

            if (fileInfo.Length < 64)
            {
                Console.WriteLine($"FAIL: Dump file too small ({fileInfo.Length} bytes), not a valid ELF file.");
                return 1;
            }

            // Read the first 18 bytes: 16-byte ELF ident + 2-byte e_type.
            byte[] header = new byte[18];
            using (var fs = File.OpenRead(dumpFile))
            {
                fs.ReadExactly(header, 0, 18);
            }

            // Check ELF magic
            if (header[0] != ElfMagic[0] || header[1] != ElfMagic[1] ||
                header[2] != ElfMagic[2] || header[3] != ElfMagic[3])
            {
                Console.WriteLine($"FAIL: Dump file does not have ELF magic. Got: {header[0]:X2} {header[1]:X2} {header[2]:X2} {header[3]:X2}");
                return 1;
            }

            // EI_CLASS: 1 = 32-bit, 2 = 64-bit
            byte elfClass = header[4];
            if (elfClass != 2)
            {
                Console.WriteLine($"FAIL: Expected 64-bit ELF (class=2), got class={elfClass}.");
                return 1;
            }

            // e_type at offset 16 in ELF64 header: ET_CORE = 4
            // EI_DATA (header[5]): 1 = little-endian, 2 = big-endian
            bool isLittleEndian = header[5] == 1;
            ushort etype = isLittleEndian
                ? (ushort)(header[16] | (header[17] << 8))
                : (ushort)((header[16] << 8) | header[17]);

            if (etype != 4)
            {
                Console.WriteLine($"FAIL: Expected ELF type ET_CORE (4), got {etype}.");
                return 1;
            }

            Console.WriteLine("PASS: Valid ELF core dump generated by linked-in createdump.");
            return 100;
        }
        finally
        {
            try
            {
                Directory.Delete(dumpDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe int CrashChild()
    {
        Console.WriteLine("Child: About to crash via null pointer dereference.");

        // Force a SIGSEGV by writing to address zero.
        *(int*)0 = 42;

        // Should not reach here.
        return 1;
    }
}
