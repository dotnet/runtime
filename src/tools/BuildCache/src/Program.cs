// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.Globalization;
using System.IO;
using Microsoft.NET.Build.Tasks;

namespace BuildCache;

internal static class Program
{
    private static readonly string[] s_cutoffFormats = { "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz" };

    private static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var command = new RootCommand("Experimental build invocation cache maintenance. Commands and cache formats may change without notice.");
        var cache = new Option<string?>("--cache")
        {
            Description = "Named cache: illink or crossgen2. Uses its path environment variable or platform default."
        };
        var directory = new Option<DirectoryInfo>("--cache-directory")
        {
            Description = "Explicit cache root, overriding named-cache settings. No builds may use this cache while purging."
        }.AcceptExistingOnly();
        var before = new Option<DateTimeOffset>("--before")
        {
            Description = "Delete entries last used before this UTC ISO 8601 timestamp (Z or +00:00).",
            Required = true,
            CustomParser = result =>
            {
                string value = result.Tokens[0].Value;
                if (DateTimeOffset.TryParseExact(value, s_cutoffFormats,
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset cutoff) &&
                    cutoff.Offset == TimeSpan.Zero)
                {
                    return cutoff;
                }

                result.AddError("--before must be a UTC ISO 8601 timestamp ending in Z or +00:00.");
                return default;
            }
        };
        var purge = new Command("purge", "Delete unused entries from an idle invocation cache.");
        purge.Options.Add(cache);
        purge.Options.Add(directory);
        purge.Options.Add(before);
        purge.SetAction(result =>
        {
            string? name = result.GetValue(cache);
            InvocationCacheConfiguration? configuration = name switch
            {
                "illink" => new("ILLink", "ILLINK_EXPERIMENTAL_CACHE", "ILLINK_EXPERIMENTAL_CACHE_PATH"),
                "crossgen2" => new("Crossgen2", "CROSSGEN2_EXPERIMENTAL_CACHE", "CROSSGEN2_EXPERIMENTAL_CACHE_PATH"),
                _ => null
            };
            if (name is not null && configuration is null)
            {
                error.WriteLine("--cache must be 'illink' or 'crossgen2'.");
                return 1;
            }

            string? path = result.GetValue(directory)?.FullName;
            if (path is null && configuration is null)
            {
                error.WriteLine("Specify --cache or --cache-directory.");
                return 1;
            }

            try
            {
                if (path is null && configuration is not null)
                {
                    path = Environment.GetEnvironmentVariable(configuration.PathEnvironmentVariable);
                    if (string.IsNullOrEmpty(path))
                        path = InvocationCacheConfiguration.GetDefaultCacheDirectory(configuration.ToolName);
                }
                if (path is null)
                {
                    error.WriteLine("No default cache directory is available; specify --cache-directory.");
                    return 1;
                }

                path = Path.GetFullPath(path);
                if (!Directory.Exists(path))
                {
                    error.WriteLine($"Cache directory '{path}' does not exist or cannot be accessed.");
                    return 1;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
            {
                error.WriteLine($"Could not resolve cache directory '{path}': {ex.Message}");
                return 1;
            }

            return InvocationCachePurge.Run(path, InvocationCacheEntry.VersionDirectory,
                configuration?.ToolName ?? "Build", result.GetValue(before), output, error);
        });
        command.Subcommands.Add(purge);
        return command.Parse(args).Invoke(new()
        {
            EnableDefaultExceptionHandler = false,
            Output = output,
            Error = error
        });
    }
}
