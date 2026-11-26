// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal static class InvocationCachePurge
{
    internal static int Run(string cacheDirectory, string versionDirectory, string toolName,
        DateTimeOffset cutoff, TextWriter output, TextWriter error)
    {
        int deleted = 0;
        int kept = 0;
        int errors = 0;
        string entriesDirectory = Path.Combine(cacheDirectory, versionDirectory);
        try
        {
            foreach (string entry in Directory.EnumerateDirectories(entriesDirectory))
            {
                if (!InvocationCacheEntry.IsEntryName(Path.GetFileName(entry)))
                    continue;

                try
                {
                    DateTimeOffset lastUsed = GetLastUsed(entry, toolName, error);

                    if (lastUsed >= cutoff)
                    {
                        kept++;
                        continue;
                    }

                    Directory.Delete(entry, recursive: true);
                    deleted++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or InvalidDataException)
                {
                    errors++;
                    error.WriteLine($"Could not purge {toolName} cache entry '{entry}': {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors++;
            error.WriteLine($"Could not enumerate {toolName} cache '{entriesDirectory}': {ex.Message}");
        }

        output.WriteLine($"{toolName} cache purge: Deleted: {deleted}, Kept: {kept}, Errors: {errors}");
        return errors == 0 ? 0 : 1;
    }

    private static DateTimeOffset GetLastUsed(string entry, string toolName, TextWriter error)
    {
        string marker = Path.Combine(entry, InvocationCacheEntry.LastUsedFileName);
        string reason = "missing or invalid";
        try
        {
            if (File.Exists(marker) &&
                DateTimeOffset.TryParseExact(File.ReadAllText(marker), "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTimeOffset lastUsed) &&
                lastUsed.Offset == TimeSpan.Zero)
            {
                return lastUsed;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            reason = ex.Message;
        }

        error.WriteLine($"Could not read a valid last-used marker for {toolName} cache entry '{entry}' ({reason}); using directory creation time.");
        return new DateTimeOffset(Directory.GetCreationTimeUtc(entry), TimeSpan.Zero);
    }
}
