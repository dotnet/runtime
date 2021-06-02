using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
        OperatingSystem.IsWindows()
            ? TryGetPhysicalMemMBWindows()
            : TryGetPhysicalMemMBNonWindows();

    private static uint? TryGetPhysicalMemMBNonWindows() {
        if (File.Exists("/proc/meminfo")) {
            string s = File.ReadAllText("/proc/meminfo");
            Regex regex = new Regex(@"MemAvailable:\s*([0-9]+)\s*kB");
            Match match = regex.Match(s);
            if (match.Success) {
                return KBToMB(ParseUint(match.Groups[1].Value));
            } 
        }
        return null;
    }
    
    private static uint ParseUint(string s) =>
        uint.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

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
}
