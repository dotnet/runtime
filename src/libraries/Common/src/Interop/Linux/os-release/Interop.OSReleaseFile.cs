// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

internal static partial class Interop
{
    // Parse information from '/etc/os-release'.
    internal static class OSReleaseFile
    {
        private const string EtcOsReleasePath = "/etc/os-release";

        /// <summary>
        /// Returns a user-friendly distribution name.
        /// </summary>
        internal static string? GetPrettyName(string filename = EtcOsReleasePath)
        {
            if (File.Exists(filename))
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(filename);
                }
                catch
                {
                    return null;
                }

                // Parse the NAME, PRETTY_NAME, and VERSION fields.
                // These fields are suitable for presentation to the user.
                string? prettyName = null, name = null, version = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
                    {
                        prettyName = line.Substring("PRETTY_NAME=".Length);
                    }
                    else if (line.StartsWith("NAME=", StringComparison.Ordinal))
                    {
                        name = line.Substring("NAME=".Length);
                    }
                    else if (line.StartsWith("VERSION=", StringComparison.Ordinal))
                    {
                        version = line.Substring("VERSION=".Length);
                    }
                }

                // Prefer PRETTY_NAME.
                if (prettyName is not null)
                {
                    return GetValue(prettyName);
                }

                // Fall back to: NAME[ VERSION].
                if (name is not null)
                {
                    if (version is not null)
                    {
                        return $"{GetValue(name)} {GetValue(version)}";
                    }
                    return GetValue(name);
                }

                static string GetValue(string fieldValue)
                {
                    // Remove enclosing quotes.
                    if ((fieldValue.StartsWith('"') && fieldValue.EndsWith('"')) ||
                        (fieldValue.StartsWith('\'') && fieldValue.EndsWith('\'')))
                    {
                        fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                    }

                    return fieldValue;
                }
            }

            return null;
        }
    }
}
