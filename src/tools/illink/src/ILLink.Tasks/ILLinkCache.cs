// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Build.Tasks;

namespace ILLink.Tasks;

internal sealed class ILLinkCache : InvocationCache
{
    private ILLinkCache(string cacheDirectory, TaskLoggingHelper log)
        : base(cacheDirectory, log, "ILLink")
    {
    }

    internal static ILLinkCache? TryCreateFromEnvironment(TaskLoggingHelper log)
    {
        string? setting = Environment.GetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE");
        if (string.IsNullOrEmpty(setting))
            return null;

        if (!bool.TryParse(setting, out bool enabled))
        {
            log.LogMessage(MessageImportance.Low, "ILLink caching is disabled because ILLINK_EXPERIMENTAL_CACHE must be 'true' or 'false'.");
            return null;
        }

        return enabled ? TryCreate(Environment.GetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH"), log) : null;
    }

    internal static ILLinkCache? TryCreate(string? cacheDirectory, TaskLoggingHelper log)
    {
        if (string.IsNullOrEmpty(cacheDirectory))
            cacheDirectory = GetDefaultCacheDirectory();

        if (cacheDirectory is null)
        {
            log.LogMessage(MessageImportance.Low, "ILLink caching is disabled because no default cache directory is available.");
            return null;
        }

        try
        {
            return new ILLinkCache(Path.GetFullPath(cacheDirectory), log);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            log.LogMessage(MessageImportance.Low, $"ILLink caching is disabled because the cache directory '{cacheDirectory}' could not be resolved: {ex.Message}");
            return null;
        }
    }

    private static string? GetDefaultCacheDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
            return string.IsNullOrEmpty(localAppData) || !Path.IsPathRooted(localAppData)
                ? null
                : Path.Combine(localAppData, "illink");
        }

        bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        if (!isMacOS)
        {
            string? cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrEmpty(cacheHome) && Path.IsPathRooted(cacheHome))
                return Path.Combine(cacheHome, "illink");
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrEmpty(home) || !Path.IsPathRooted(home))
            return null;

        return isMacOS
            ? Path.Combine(home, "Library", "Caches", "illink")
            : Path.Combine(home, ".cache", "illink");
    }
}
