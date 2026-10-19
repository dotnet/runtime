// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace HelixCache;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            if (args is ["--help"] or ["-h"])
            {
                Console.WriteLine(SR.Get("Usage"));
                return 0;
            }
            if (!OperatingSystem.IsLinux())
            {
                throw new PlatformNotSupportedException(SR.Get("UnsupportedPlatform"));
            }
            if (args.Length < 2 || args[0] is not ("lookup" or "store") || args.Length % 2 != 0)
            {
                throw new InvalidDataException(SR.Get("Usage"));
            }

            Dictionary<string, string> options = new(StringComparer.Ordinal);
            for (int i = 2; i < args.Length; i += 2)
            {
                if (!options.TryAdd(args[i], args[i + 1]) || string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    throw new InvalidDataException(SR.Get("Usage"));
                }
            }

            Cache cache = new(Take(options, "--cache"));
            string manifest = Path.GetFullPath(args[1]);
            if (args[0] == "lookup")
            {
                string output = Take(options, "--output");
                options.Remove("--queue-info", out string? queueInfo);
                JsonFile.Require(options.Count == 0, nameof(options));
                Submission submission = JsonFile.Read(manifest, CacheJsonContext.Default.Submission);
                using LinuxEnvironment environment = await LinuxEnvironment.LoadAsync(queueInfo);
                LookupResult result = cache.Lookup(submission, Path.GetDirectoryName(manifest)!, environment);
                JsonFile.Write(output, result, CacheJsonContext.Default.LookupResult);
            }
            else
            {
                string results = Take(options, "--results");
                JsonFile.Require(options.Count == 0, nameof(options));
                ExecutionRecord execution = JsonFile.Read(manifest, CacheJsonContext.Default.ExecutionRecord);
                cache.Store(execution, results);
                Console.Error.WriteLine(SR.Get("Stored", execution.Key));
            }
            return 0;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or
            HttpRequestException or TaskCanceledException or XmlException or ArgumentException or
            InvalidOperationException or KeyNotFoundException or OverflowException or PlatformNotSupportedException)
        {
            Console.Error.WriteLine(SR.Get("Error", ex.Message));
            return 1;
        }
    }

    private static string Take(Dictionary<string, string> options, string name)
    {
        if (!options.Remove(name, out string? value))
        {
            throw new InvalidDataException(SR.Get("Usage"));
        }
        return value;
    }
}
