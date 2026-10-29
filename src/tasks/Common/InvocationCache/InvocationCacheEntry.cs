// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal static class InvocationCacheEntry
{
    internal const int FormatVersion = 1;
    internal const string VersionDirectory = "v1";
    internal const string LastUsedFileName = "last-used";

    internal static bool IsEntryName(string name)
    {
        if (name.Length != 64)
            return false;

        foreach (char c in name)
        {
            if (c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
                return false;
        }

        return true;
    }

    internal static void WriteLastUsed(string path) =>
        File.WriteAllText(path, System.DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
}
