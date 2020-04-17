// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        // Order of these properties in Windows matter because GetUseIcuMode is dependent on Invariant.
        // So we need Invariant to be initialized first.
        internal static bool Invariant { get; } = GetInvariantSwitchValue();

        internal static bool UseNls { get; } = !Invariant &&
            (GetSwitchValue("System.Globalization.UseNls", "DOTNET_SYSTEM_GLOBALIZATION_USENLS") ||
                !LoadIcu());

        private static bool LoadIcu()
        {
            if (!TryGetAppLocalIcuSwitchValue(out ReadOnlySpan<char> version, out ReadOnlySpan<char> suffix))
            {
                return Interop.Globalization.LoadICU() != 0;
            }

            string extension = ".dll";
            string icuucBase = "icuuc";
            string icuinBase = "icuin";
            IntPtr icuucLib = IntPtr.Zero;
            IntPtr icuinLib = IntPtr.Zero;
            Assembly assembly = Assembly.GetExecutingAssembly();

            int index = version.ToString().IndexOf('.', StringComparison.Ordinal);

            if (index != -1)
            {
                ReadOnlySpan<char> truncatedVersion = version.Slice(0, index);
                icuucLib = LoadLibrary(CreateLibraryName(icuucBase, suffix, extension, truncatedVersion), assembly, failOnLoadFailure: false);

                if (icuucLib != IntPtr.Zero)
                {
                    icuinLib = LoadLibrary(CreateLibraryName(icuinBase, suffix, extension, truncatedVersion), assembly, failOnLoadFailure: false);
                }
            }

            if (icuucLib == IntPtr.Zero)
            {
                icuucLib = LoadLibrary(CreateLibraryName(icuucBase, suffix, extension, version), assembly, failOnLoadFailure: true);
            }

            if (icuinLib == IntPtr.Zero)
            {
                icuinLib = LoadLibrary(CreateLibraryName(icuinBase, suffix, extension, version), assembly, failOnLoadFailure: true);
            }

            Interop.Globalization.InitICU(icuucLib, icuinLib, version.ToString(), suffix.Length > 0 ? suffix.ToString() : null);
            return true;
        }
    }
}
