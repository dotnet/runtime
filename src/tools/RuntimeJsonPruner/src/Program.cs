// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace RuntimeJsonPruner;

public static class Program
{
    // Host RIDs that must be kept (based on host algorithm and design doc)
    private static readonly HashSet<string> HostRids = new(StringComparer.Ordinal)
    {
        // Base portable RIDs
        "win",
        "unix",
        "linux",
        "osx",

        // Windows host RIDs
        "win-x86",
        "win-x64",
        "win-arm64",

        // macOS host RIDs
        "osx-x64",
        "osx-arm64",

        // Linux glibc host RIDs
        "linux-x86",
        "linux-x64",
        "linux-arm",
        "linux-arm64",

        // Linux musl host RIDs
        "linux-musl",
        "linux-musl-x64",
        "linux-musl-arm64",
        "linux-musl-x86",
        "linux-musl-arm",
        "linux-musl-armv6",
        "linux-musl-ppc64le",
        "linux-musl-s390x",
        "linux-musl-riscv64",
        "linux-musl-loongarch64",

        // Linux bionic (Android) host RIDs
        "linux-bionic",
        "linux-bionic-x86",
        "linux-bionic-x64",
        "linux-bionic-arm",
        "linux-bionic-arm64",

        // Additional portable Linux architectures
        "linux-s390x",
        "linux-ppc64le",
        "linux-riscv64",
        "linux-loongarch64",
        "linux-armv6",

        // Unversioned Apple platform RIDs that might be referenced
        "ios",
        "ios-arm64",
        "tvos",
        "tvos-arm64",
        "maccatalyst",
        "maccatalyst-x64",
        "maccatalyst-arm64",

        // Unversioned non-Linux Unix RIDs
        "freebsd",
        "freebsd-x64",
        "freebsd-arm64",
        "illumos",
        "illumos-x64",
        "solaris",
        "solaris-x64",
    };

    // Linux distro prefixes to remove
    private static readonly string[] LinuxDistrosPrefixes = new[]
    {
        "alpine",
        "ubuntu",
        "debian",
        "centos",
        "rhel",
        "fedora",
        "sles",
        "opensuse",
        "ol",
        "oraclelinux",
        "linuxmint",
        "tizen",
    };

    // Unix OS families that might have versioned RIDs
    private static readonly string[] UnixOsFamilies = new[]
    {
        "freebsd",
        "illumos",
        "solaris",
    };

    // Apple OS families that might have versioned RIDs
    private static readonly string[] AppleOsFamilies = new[]
    {
        "osx",
        "ios",
        "tvos",
        "maccatalyst",
    };

    public static int Main(string[] args)
    {
        string runtimeJsonPath = args.Length > 0
            ? args[0]
            : Path.Combine("src", "libraries", "Microsoft.NETCore.Platforms", "src", "runtime.json");

        if (!File.Exists(runtimeJsonPath))
        {
            Console.Error.WriteLine($"Error: File not found: {runtimeJsonPath}");
            return 1;
        }

        Console.WriteLine($"Processing: {runtimeJsonPath}");
        Console.WriteLine();

        try
        {
            string jsonContent = File.ReadAllText(runtimeJsonPath);
            JsonNode? root = JsonNode.Parse(jsonContent);

            if (root is not JsonObject rootObj || root["runtimes"] is not JsonObject runtimes)
            {
                Console.Error.WriteLine("Error: Invalid runtime.json format");
                return 1;
            }

            int originalCount = runtimes.Count;
            var stats = new PruningStats();

            // Collect RIDs to remove
            var ridsToRemove = new HashSet<string>(StringComparer.Ordinal);

            foreach (var kvp in runtimes.ToList())
            {
                string rid = kvp.Key;

                if (ShouldRemove(rid, out string? reason))
                {
                    ridsToRemove.Add(rid);
                    stats.RecordRemoval(reason!);
                }
            }

            // Remove the RIDs
            foreach (string rid in ridsToRemove)
            {
                runtimes.Remove(rid);
            }

            // Clean up #import arrays
            foreach (var kvp in runtimes.ToList())
            {
                if (kvp.Value is JsonObject ridObj && ridObj["#import"] is JsonArray imports)
                {
                    var toRemove = new List<int>();

                    for (int i = imports.Count - 1; i >= 0; i--)
                    {
                        string? importedRid = imports[i]?.GetValue<string>();
                        if (importedRid != null && ridsToRemove.Contains(importedRid))
                        {
                            toRemove.Add(i);
                        }
                    }

                    foreach (int index in toRemove)
                    {
                        imports.RemoveAt(index);
                    }
                }
            }

            // Write the modified JSON back
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string modifiedJson = root.ToJsonString(options);
            File.WriteAllText(runtimeJsonPath, modifiedJson + "\n");

            // Print statistics
            int finalCount = runtimes.Count;
            Console.WriteLine($"Original RID count: {originalCount}");
            Console.WriteLine($"Final RID count:    {finalCount}");
            Console.WriteLine($"Removed:            {originalCount - finalCount}");
            Console.WriteLine();
            Console.WriteLine("Breakdown of removed RIDs:");
            stats.PrintStats();

            // Verify no Alpine RIDs remain
            bool hasAlpine = runtimes.Any(kvp => kvp.Key.StartsWith("alpine", StringComparison.Ordinal));
            if (hasAlpine)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("WARNING: Alpine RIDs still present in the output!");
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("✓ Success: No Alpine RIDs remain in runtime.json");
            Console.WriteLine($"✓ File updated: {runtimeJsonPath}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static bool ShouldRemove(string rid, out string? reason)
    {
        reason = null;

        // Keep all host RIDs
        if (HostRids.Contains(rid))
        {
            return false;
        }

        // Keep all Windows RIDs (win, win-, win.)
        if (rid == "win" || rid.StartsWith("win-", StringComparison.Ordinal) || rid.StartsWith("win.", StringComparison.Ordinal))
        {
            return false;
        }

        // Remove Linux distro-specific RIDs
        foreach (string distro in LinuxDistrosPrefixes)
        {
            if (rid.StartsWith(distro, StringComparison.Ordinal))
            {
                reason = $"Linux distro: {distro}";
                return true;
            }
        }

        // Remove versioned Apple RIDs (contains a dot after the family name)
        foreach (string family in AppleOsFamilies)
        {
            if (rid.StartsWith(family + ".", StringComparison.Ordinal))
            {
                reason = $"Versioned Apple: {family}";
                return true;
            }
        }

        // Remove versioned non-Linux Unix RIDs (contains a dot after the family name)
        foreach (string family in UnixOsFamilies)
        {
            if (rid.StartsWith(family + ".", StringComparison.Ordinal))
            {
                reason = $"Versioned Unix: {family}";
                return true;
            }
        }

        // Keep everything else (including unversioned Unix RIDs, other architectures, etc.)
        return false;
    }

    private sealed class PruningStats
    {
        private readonly Dictionary<string, int> _counts = new();

        public void RecordRemoval(string category)
        {
            _counts.TryGetValue(category, out int count);
            _counts[category] = count + 1;
        }

        public void PrintStats()
        {
            foreach (var kvp in _counts.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"  {kvp.Key,-30} {kvp.Value,5} RIDs");
            }
        }
    }
}
