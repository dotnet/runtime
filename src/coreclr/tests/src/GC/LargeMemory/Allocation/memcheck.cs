using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#nullable enable

public static class MemCheck {
    public static uint? TryGetPhysicalMemMB() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? TryGetPhysicalMemMBWindows()
            : TryGetPhysicalMemMBNonWindows();

    private static uint? TryGetPhysicalMemMBNonWindows() {
        string? kb = File.Exists("/proc/meminfo")
            ? TryExtractLine(File.ReadAllText("/proc/meminfo"), prefix: "MemAvailable", suffix: "kB")
            : null;
        return kb == null ? (uint?) null : KBToMB(uint.Parse(kb));
    }

    private static uint KBToMB(uint i) =>
         i / 1024;

    private static uint? TryGetPhysicalMemMBWindows()
    {
        string? mb = TryExtractLine(RunCommand("systeminfo"), prefix: "Total Physical Memory:", suffix: "MB");
        return mb == null ? (uint?) null : uint.Parse(mb);
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
