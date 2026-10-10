// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO;

namespace ILLink.Tasks;

internal static class ILLinkCacheEntry
{
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
