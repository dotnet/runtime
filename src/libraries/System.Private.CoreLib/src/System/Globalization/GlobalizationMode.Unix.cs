// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        // Order of these properties in Windows matter because GetUseIcuMode is dependent on Invariant.
        // So we need Invariant to be initialized first.
        internal static bool Invariant { get; } = GetGlobalizationInvariantMode();

        internal static bool UseNls => false;

        private static bool GetGlobalizationInvariantMode()
        {
            bool invariantEnabled = GetInvariantSwitchValue();
            if (!invariantEnabled)
            {
                if (TryGetAppLocalIcuSwitchValue(out string? icuSuffixAndVersion))
                {
                    LoadAppLocalIcu(icuSuffixAndVersion, suffixWithSeparator: true);
                }
                else if (Interop.Globalization.LoadICU() == 0)
                {
                    string message = "Couldn't find a valid ICU package installed on the system. " +
                                    "Set the configuration flag System.Globalization.Invariant to true if you want to run with no globalization support.";
                    Environment.FailFast(message);
                }
            }
            return invariantEnabled;
        }

        private static void LoadAppLocalIcuCore(ReadOnlySpan<char> version, ReadOnlySpan<char> suffix)
        {

#if TARGET_OSX
            const string extension = ".dylib";
            bool versionAtEnd = false;
#else
            string extension = version.Length > 0 ? "so." : "so";
            bool versionAtEnd = true;
#endif

#if !TARGET_OSX
            // In Linux we need to load libicudata first because libicuuc and libicui18n depend on it. In order for the loader to find
            // it on the same path, we load it before loading the other two libraries.
            LoadLibrary(CreateLibraryName("libicudata", suffix, extension, version, versionAtEnd), failOnLoadFailure: true);
#endif

            IntPtr icuucLib = LoadLibrary(CreateLibraryName("libicuuc", suffix, extension, version, versionAtEnd), failOnLoadFailure: true);
            IntPtr icuinLib = LoadLibrary(CreateLibraryName("libicui18n", suffix, extension, version, versionAtEnd), failOnLoadFailure: true);

            Interop.Globalization.InitICUFunctions(icuucLib, icuinLib, version, suffix);
        }
    }
}
