// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        internal static bool UseNls { get; } = !Invariant &&
            (AppContextConfigHelper.GetBooleanConfig("System.Globalization.UseNls", "DOTNET_SYSTEM_GLOBALIZATION_USENLS") ||
                !LoadIcu());

        private static bool LoadIcu()
        {
            if (!TryGetAppLocalIcuSwitchValue(out string? icuSuffixAndVersion))
            {
                return Interop.Globalization.LoadICU() != 0;
            }

            LoadAppLocalIcu(icuSuffixAndVersion);
            return true;
        }

        private static void LoadAppLocalIcuCore(ReadOnlySpan<char> version, ReadOnlySpan<char> suffix)
        {
            const string extension = ".dll";
            const string icuucBase = "icuuc";
            const string icuinBase = "icuin";
            nint icuucLib = 0;
            nint icuinLib = 0;

            int index = version.IndexOf('.');
            if (index > 0)
            {
                ReadOnlySpan<char> truncatedVersion = version.Slice(0, index);
                icuucLib = LoadLibrary(CreateLibraryName(icuucBase, suffix, extension, truncatedVersion), failOnLoadFailure: false);

                if (icuucLib != 0)
                {
                    icuinLib = LoadLibrary(CreateLibraryName(icuinBase, suffix, extension, truncatedVersion), failOnLoadFailure: false);
                }
            }

            if (icuucLib == 0)
            {
                icuucLib = LoadLibrary(CreateLibraryName(icuucBase, suffix, extension, version), failOnLoadFailure: true);
            }

            if (icuinLib == 0)
            {
                icuinLib = LoadLibrary(CreateLibraryName(icuinBase, suffix, extension, version), failOnLoadFailure: true);
            }

            Interop.Globalization.InitICUFunctions(icuucLib, icuinLib, version, suffix);
        }
    }
}
