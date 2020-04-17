// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

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
                if (!LoadIcu())
                {
                    string message = "Couldn't find a valid ICU package installed on the system. " +
                                    "Set the configuration flag System.Globalization.Invariant to true if you want to run with no globalization support.";
                    Environment.FailFast(message);
                }
            }
            return invariantEnabled;
        }

        private static bool LoadIcu()
        {
            if (!TryGetAppLocalIcuSwitchValue(out ReadOnlySpan<char> version, out ReadOnlySpan<char> suffix))
            {
                return Interop.Globalization.LoadICU() != 0;
            }

#if TARGET_OSX
            string extension = ".dylib";
            bool versionAtEnd = false;
#else
            string extension = "so.";
            bool versionAtEnd = true;
#endif

            // Append '.' to suffix since in Unix the version is separated by '.'
            int suffixLength = suffix.Length + 1;
            Span<char> suffixWithSeparator = stackalloc char[suffixLength];
            suffix.CopyTo(suffixWithSeparator);
            suffixWithSeparator[suffixLength - 1] = '.';

            Assembly assembly = Assembly.GetExecutingAssembly();

#if !TARGET_OSX
            // In Linux we need to load libicudata first because libicuuc and libicui18n depend on it. In order for the loader to find
            // it on the same path, we load it before loading the other two libraries.
            string icudataBase = "libicudata";
            LoadLibrary(CreateLibraryName(icudataBase, suffixWithSeparator, extension, version, versionAtEnd), assembly, failOnLoadFailure: true);
#endif

            string icuucBase = "libicuuc";
            string icuinBase = "libicui18n";
            IntPtr icuucLib = LoadLibrary(CreateLibraryName(icuucBase, suffixWithSeparator, extension, version, versionAtEnd), assembly, failOnLoadFailure: true);
            IntPtr icuinLib = LoadLibrary(CreateLibraryName(icuinBase, suffixWithSeparator, extension, version, versionAtEnd), assembly, failOnLoadFailure: true);

            Interop.Globalization.InitICU(icuucLib, icuinLib, version.ToString(), suffix.Length > 0 ? suffix.ToString() : null);
            return true;
        }
    }
}
