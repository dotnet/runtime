using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#nullable enable

public static class MemCheck {
    public static uint ParseSizeMBAndLimitByAvailableMem(IReadOnlyList<string> args) =>
        LimitByAvailableMemMB(ParseSizeMBArgument(args));

    public static uint ParseSizeMBArgument(IReadOnlyList<string> args) {
        try {
            return ParseUint(args[0]);
        } catch (Exception e) {
            if ( (e is IndexOutOfRangeException) || (e is FormatException) || (e is OverflowException) ) {
                throw new Exception("args: uint - number of MB to allocate");
            }
            throw;
        }
    }

    public static uint LimitByAvailableMemMB(uint sizeMB, uint defaultMB = 300)
    {
        uint? availableMem = TryGetPhysicalMemMB();
        if (availableMem != null && availableMem < sizeMB){
            uint mb = availableMem > defaultMB ? defaultMB : (availableMem.Value / 2);
            Console.WriteLine($"Not enough memory. Allocating {mb}MB instead.");
            return mb;
        } else {
            return sizeMB;
        }
    }

    private static uint? TryGetPhysicalMemMB() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? TryGetPhysicalMemMBWindows()
            : TryGetPhysicalMemMBNonWindows();

    private static uint? TryGetPhysicalMemMBNonWindows() {
        string? kb = File.Exists("/proc/meminfo")
            ? TryExtractLine(File.ReadAllText("/proc/meminfo"), prefix: "MemAvailable:", suffix: "kB")
            : null;
        return kb == null ? (uint?) null : KBToMB(ParseUint(kb));
    }

    private static uint KBToMB(uint kb) =>
         kb / 1024;

    private static uint BytesToMB(ulong bytes) =>
        (uint) (bytes / (1024 * 1024));

    private static uint? TryGetPhysicalMemMBWindows()
    {
        MEMORYSTATUSEX mem = new MEMORYSTATUSEX
        {
            dwLength = (uint) Marshal.SizeOf(typeof(MEMORYSTATUSEX))
        };
        bool success = GlobalMemoryStatusEx(ref mem);
        return success ? BytesToMB(mem.ullAvailPhys) : (uint?) null;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    // https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/ns-sysinfoapi-memorystatusex
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    private static string? TryExtractLine(string s, string prefix, string suffix) =>
        FirstNonNull(from line in Lines(s) select TryRemoveStringStartEnd(line, prefix, suffix));

    private static T? FirstNonNull<T>(IEnumerable<T?> xs) where T : class =>
        xs.First(x => x != null);

    private static string? TryRemoveStringStartEnd(string s, string start, string end) {
        int newLength = s.Length - (start.Length + end.Length);
        return newLength > 0 && s.StartsWith(start) && s.EndsWith(end)
            ? s.Substring(start.Length, newLength)
            : null;
    }

    private static uint ParseUint(string s) =>
        uint.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

    private static string RunCommand(string name) {
        ProcessStartInfo startInfo = new ProcessStartInfo(name) {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        using (Process cmd = new Process() { StartInfo = startInfo }) {
            cmd.Start();
            cmd.WaitForExit();
            return cmd.StandardOutput.ReadToEnd();
        }
    }

    private static IEnumerable<string> Lines(string s) {
        using (StringReader reader = new StringReader(s)) {
            while (true) {
                string? line = reader.ReadLine();
                if (line == null) {
                    break;
                }
                yield return line;
            }
        }
    }
}
