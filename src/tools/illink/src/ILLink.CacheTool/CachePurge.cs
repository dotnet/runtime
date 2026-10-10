// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using ILLink.Tasks;

namespace ILLink.CacheTool;

internal static class CachePurge
{
    internal static int Run(string cacheDirectory, DateTimeOffset cutoff, TextWriter output, TextWriter error)
    {
        int deleted = 0;
        int kept = 0;
        int errors = 0;
        string entriesDirectory = Path.Combine(cacheDirectory, ILLinkCacheEntry.VersionDirectory);
        try
        {
            foreach (string entry in Directory.EnumerateDirectories(entriesDirectory))
            {
                if (!ILLinkCacheEntry.IsEntryName(Path.GetFileName(entry)))
                    continue;

                try
                {
                    DateTimeOffset lastUsed = GetLastUsed(entry, error);

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
                    error.WriteLine($"Could not purge ILLink cache entry '{entry}': {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors++;
            error.WriteLine($"Could not enumerate ILLink cache '{entriesDirectory}': {ex.Message}");
        }

        output.WriteLine($"ILLink cache purge: Deleted: {deleted}, Kept: {kept}, Errors: {errors}");
        return errors == 0 ? 0 : 1;
    }

    private static DateTimeOffset GetLastUsed(string entry, TextWriter error)
    {
        string marker = Path.Combine(entry, ILLinkCacheEntry.LastUsedFileName);
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

        error.WriteLine($"Could not read a valid last-used marker for ILLink cache entry '{entry}' ({reason}); using directory creation time.");
        return new DateTimeOffset(Directory.GetCreationTimeUtc(entry), TimeSpan.Zero);
    }
}
