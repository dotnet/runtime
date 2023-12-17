// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop
{
    // This type is a record to get the generated equality and hashing operators
    // which will be faster than the reflection-based ones.
    public readonly record struct TargetFrameworkSettings(TargetFramework TargetFramework, Version Version);

    /// <summary>
    /// Target framework identifier
    /// </summary>
    public enum TargetFramework
    {
        Unknown,
        Framework,
        Core,
        Standard,
        Net
    }

    public static class TargetFrameworkSettingsExtensions
    {
        private static readonly Version FirstNonCoreVersion = new(5, 0);

        // Parse from the informational version as that is the only version that always matches the TFM version
        // even in debug builds.
        private static readonly Version ThisAssemblyVersion = Version.Parse(
            typeof(TargetFrameworkSettingsExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('-', '+')[0]);

        public static TargetFrameworkSettings GetTargetFrameworkSettings(this AnalyzerConfigOptions options)
        {
            // Our generator only runs in the following scenarios:
            // - In the dotnet/runtime repository.
            // - In a .NET SDK for the same TFM that matches the version of this assembly.
            // We'll try to pull the TFM information from the build, but if it is not present,
            // then we'll assume we're in the ref pack as the TFM information will always be present in the dotnet/runtime build.
            options.TryGetValue("build_property.TargetFrameworkIdentifier", out string? frameworkIdentifier);
            options.TryGetValue("build_property.TargetFrameworkVersion", out string? versionString);
            // TargetFrameworkVersion starts with a 'v'.
            Version? version = versionString is not null ? Version.Parse(versionString.Substring(1)) : null;
            return new TargetFrameworkSettings(
                frameworkIdentifier switch
                {
                    ".NETStandard" => TargetFramework.Standard,
                    ".NETCoreApp" when version is not null && version < FirstNonCoreVersion => TargetFramework.Core,
                    ".NETCoreApp" => TargetFramework.Net,
                    // If the TFM is not specified, we'll infer it from this assembly.
                    // Since we only ship this assembly as part of the Microsoft.NETCore.App TFM,
                    // the down-level support only matters for the repo where this project is built.
                    // In all other cases, we will only be used from the TFM with the matching version as our assembly.
                    null => TargetFramework.Net,
                    // Assume that all unknown target framework identifiers are .NET Framework.
                    // All legacy target frameworks will have effectively the same feature set as we provide for .NET Framework
                    // for our purposes.
                    _ => TargetFramework.Framework
                },
                // If the version is not specified, we'll infer it from this assembly.
                version ?? ThisAssemblyVersion);
        }
    }
}
