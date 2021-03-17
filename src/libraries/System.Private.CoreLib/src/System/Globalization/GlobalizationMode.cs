// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static bool GetInvariantSwitchValue() =>
            AppContextConfigHelper.GetBooleanConfig("System.Globalization.Invariant", "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");

        private static bool TryGetAppLocalIcuSwitchValue([NotNullWhen(true)] out string? value) =>
            TryGetStringValue("System.Globalization.AppLocalIcu", "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", out value);

        internal static bool PredefinedCulturesOnly { get; } =
            AppContextConfigHelper.GetBooleanConfig("System.Globalization.PredefinedCulturesOnly", "DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY");

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
