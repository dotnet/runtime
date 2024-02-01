// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        // Split from GlobalizationMode so the whole class can be trimmed when Invariant=true. Trimming tests
        // validate this implementation detail.
        private static partial class Settings
        {
            internal static bool Invariant { get; } = AppContextConfigHelper.GetBooleanConfig("System.Globalization.Invariant", "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");

#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
            internal static bool Hybrid { get; } = AppContextConfigHelper.GetBooleanConfig("System.Globalization.Hybrid", "DOTNET_SYSTEM_GLOBALIZATION_HYBRID", true);
#elif TARGET_BROWSER
            internal static bool Hybrid { get; } = AppContextConfigHelper.GetBooleanConfig("System.Globalization.Hybrid", "DOTNET_SYSTEM_GLOBALIZATION_HYBRID");
#endif
            internal static bool PredefinedCulturesOnly { get; } = AppContextConfigHelper.GetBooleanConfig("System.Globalization.PredefinedCulturesOnly", "DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", GlobalizationMode.Invariant);
        }

        // Note: Invariant=true and Invariant=false are substituted at different levels in the ILLink.Substitutions file.
        // This allows for the whole Settings nested class to be trimmed when Invariant=true, and allows for the Settings
        // static cctor (on Unix) to be preserved when Invariant=false.
        internal static bool Invariant => Settings.Invariant;
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS || TARGET_BROWSER
        internal static bool Hybrid => Settings.Hybrid;
#endif
        internal static bool PredefinedCulturesOnly => Settings.PredefinedCulturesOnly;

        private static bool TryGetAppLocalIcuSwitchValue([NotNullWhen(true)] out string? value) =>
            TryGetStringValue("System.Globalization.AppLocalIcu", "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", out value);
        private static bool TryGetStringValue(string switchName, string envVariable, [NotNullWhen(true)] out string? value)
        {
            value = AppContext.GetData(switchName) as string;
            if (string.IsNullOrEmpty(value))
            {
                value = Environment.GetEnvironmentVariable(envVariable);
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
            }

            return true;
        }

        private static void LoadAppLocalIcu(string icuSuffixAndVersion)
        {
            ReadOnlySpan<char> version;
            ReadOnlySpan<char> icuSuffix = default;

            // Custom built ICU can have a suffix on the name, i.e: libicuucmyapp.so.67.1
            // So users would set the runtime switch as: myapp:67.1
            int indexOfSeparator = icuSuffixAndVersion.IndexOf(':');
            if (indexOfSeparator >= 0)
            {
                icuSuffix = icuSuffixAndVersion.AsSpan(0, indexOfSeparator);
                version = icuSuffixAndVersion.AsSpan(icuSuffix.Length + 1);
            }
            else
            {
                version = icuSuffixAndVersion;
            }

            LoadAppLocalIcuCore(version, icuSuffix);
        }

        private static string CreateLibraryName(ReadOnlySpan<char> baseName, ReadOnlySpan<char> suffix, ReadOnlySpan<char> extension, ReadOnlySpan<char> version, bool versionAtEnd = false) =>
            versionAtEnd ?
                string.Concat(baseName, suffix, extension, version) :
                string.Concat(baseName, suffix, version, extension);

        private static IntPtr LoadLibrary(string library, bool failOnLoadFailure)
        {
            if (!NativeLibrary.TryLoad(library, typeof(object).Assembly, DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.System32, out IntPtr lib) && failOnLoadFailure)
            {
                Environment.FailFast($"Failed to load app-local ICU: {library}");
            }

            return lib;
        }
    }
}
