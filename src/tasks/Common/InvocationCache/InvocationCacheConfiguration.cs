// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationCacheConfiguration
{
    internal string ToolName { get; }
    internal string EnabledEnvironmentVariable { get; }
    internal string PathEnvironmentVariable { get; }

    internal InvocationCacheConfiguration(string toolName, string enabledEnvironmentVariable, string pathEnvironmentVariable)
    {
        ToolName = toolName;
        EnabledEnvironmentVariable = enabledEnvironmentVariable;
        PathEnvironmentVariable = pathEnvironmentVariable;
    }

    internal static string? GetDefaultCacheDirectory(string toolName)
    {
        string cacheDirectoryName = toolName.ToLowerInvariant();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
            return string.IsNullOrEmpty(localAppData) || !Path.IsPathRooted(localAppData)
                ? null
                : Path.Combine(localAppData, cacheDirectoryName);
        }

        bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        if (!isMacOS)
        {
            string? cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrEmpty(cacheHome) && Path.IsPathRooted(cacheHome))
                return Path.Combine(cacheHome, cacheDirectoryName);
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrEmpty(home) || !Path.IsPathRooted(home))
            return null;

        return isMacOS
            ? Path.Combine(home, "Library", "Caches", cacheDirectoryName)
            : Path.Combine(home, ".cache", cacheDirectoryName);
    }
}
