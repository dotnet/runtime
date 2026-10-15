// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.NET.Build.Tasks;

namespace ILLink.Tasks;

internal static class ILLinkCacheEntry
{
    internal const string VersionDirectory = InvocationCacheEntry.VersionDirectory;
    internal const string LastUsedFileName = InvocationCacheEntry.LastUsedFileName;

    internal static bool IsEntryName(string name) => InvocationCacheEntry.IsEntryName(name);

    internal static void WriteLastUsed(string path) => InvocationCacheEntry.WriteLastUsed(path);
}
