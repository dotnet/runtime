// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static bool GetInvariantSwitchValue() =>
            GetSwitchValue("System.Globalization.Invariant", "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");

        private static bool TryGetAppLocalIcuSwitchValue(out ReadOnlySpan<char> version, out ReadOnlySpan<char> icuSuffix)
        {
            icuSuffix = default;

            if (!TryGetStringValue("System.Globalization.AppLocalIcu", "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", out string? value))
            {
                version = default;
                return false;
            }

            // Custom built ICU can have a suffix on the name, i.e: libicuucmyapp.so.67.1
            // So users would set the runtime switch as: myapp:67.1

            int indexOfSeparator = value.IndexOf(':', StringComparison.Ordinal);
            if (indexOfSeparator != -1)
            {
                ReadOnlySpan<char> valueAsSpan = value.AsSpan();
                icuSuffix = valueAsSpan.Slice(0, indexOfSeparator);

                if (icuSuffix.Length > 20)
                {
                    Environment.FailFast($"The resolved \"{icuSuffix.ToString()}\" suffix from System.Globalization.AppLocalIcu switch has to be < 20 chars long.");
                }

                version = valueAsSpan.Slice(icuSuffix.Length + 1);
            }
            else
            {
                version = value;
            }

            if (version.Length > 33)
            {
                Environment.FailFast($"The resolved version \"{version.ToString()}\" from System.Globalization.AppLocalIcu switch has to be < 33 chars long.");
            }

            return true;
        }

        // GetSwitchValue calls CLRConfig first to detect if the switch is defined in the config file.
        // if the switch is defined we just use the value of this switch. otherwise, we'll try to get the switch
        // value from the environment variable if it is defined.
        private static bool GetSwitchValue(string switchName, string envVariable)
        {
            if (!AppContext.TryGetSwitch(switchName, out bool ret))
            {
                string? switchValue = Environment.GetEnvironmentVariable(envVariable);
                if (switchValue != null)
                {
                    ret = bool.IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1");
                }
            }

            return ret;
        }

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

        private static string CreateLibraryName(ReadOnlySpan<char> baseName, ReadOnlySpan<char> suffix, ReadOnlySpan<char> extension, ReadOnlySpan<char> version, bool versionAtEnd = false)
        {
            int length = baseName.Length + suffix.Length + version.Length + extension.Length;

            // We validate that suffix and version are not larger than 53 characters.
            Span<char> result = stackalloc char[length];
            baseName.CopyTo(result);

            Span<char> secondPart = result.Slice(baseName.Length);
            suffix.CopyTo(secondPart);

            Span<char> middle = secondPart.Slice(suffix.Length);

            if (!versionAtEnd)
            {
                version.CopyTo(middle);

                Span<char> end = middle.Slice(version.Length);
                extension.CopyTo(end);
            }
            else
            {
                extension.CopyTo(middle);

                Span<char> end = middle.Slice(extension.Length);
                version.CopyTo(end);
            }

            return result.ToString();
        }

        private static IntPtr LoadLibrary(string library, Assembly assembly, bool failOnLoadFailure)
        {
            if (!NativeLibrary.TryLoad(library, assembly, DllImportSearchPath.ApplicationDirectory, out IntPtr lib) && failOnLoadFailure)
            {
                Environment.FailFast($"Failed to load app-local ICU: {library}");
            }

            return lib;
        }
    }
}
