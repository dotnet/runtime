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
                    DateTimeOffset lastUsed = DateTimeOffset.ParseExact(
                        File.ReadAllText(Path.Combine(entry, ILLinkCacheEntry.LastUsedFileName)),
                        "O", CultureInfo.InvariantCulture);
                    if (lastUsed.Offset != TimeSpan.Zero)
                        throw new InvalidDataException("The last-used timestamp must be UTC.");

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
}
