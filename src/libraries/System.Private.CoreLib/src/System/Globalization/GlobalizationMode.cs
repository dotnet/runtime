// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static bool GetInvariantSwitchValue() =>
            GetSwitchValue("System.Globalization.Invariant", "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");

        private static bool TryGetAppLocalIcuSwitchValue([NotNullWhen(true)] out string? value) =>
            TryGetStringValue("System.Globalization.AppLocalIcu", "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", out value);

        private static void LoadAppLocalIcu(string icuSuffixAndVersion, bool suffixWithSeparator = false)
        {
            ReadOnlySpan<char> version;
            ReadOnlySpan<char> icuSuffix = default;

            // Custom built ICU can have a suffix on the name, i.e: libicuucmyapp.so.67.1
            // So users would set the runtime switch as: myapp:67.1
            int indexOfSeparator = icuSuffixAndVersion.IndexOf(':');
            if (indexOfSeparator >= 0)
            {
                icuSuffix = icuSuffixAndVersion.AsSpan().Slice(0, indexOfSeparator);
                version = icuSuffixAndVersion.AsSpan().Slice(icuSuffix.Length + 1);
            }
            else
            {
                version = icuSuffixAndVersion;
            }

            if (suffixWithSeparator)
            {
                icuSuffix = string.Concat(icuSuffix, ".");
            }

            LoadAppLocalIcuCore(version, icuSuffix);
        }

        private static string CreateLibraryName(ReadOnlySpan<char> baseName, ReadOnlySpan<char> suffix, ReadOnlySpan<char> extension, ReadOnlySpan<char> version, bool versionAtEnd = false) =>
            versionAtEnd ?
                string.Concat(baseName, suffix, extension, version) :
                string.Concat(baseName, suffix, version, extension);

        private static IntPtr LoadLibrary(string library, bool failOnLoadFailure)
        {
            if (!NativeLibrary.TryLoad(library, typeof(object).Assembly, DllImportSearchPath.ApplicationDirectory, out IntPtr lib) && failOnLoadFailure)
            {
                Environment.FailFast($"Failed to load app-local ICU: {library}");
            }

            return lib;
        }
    }
}
