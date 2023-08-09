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
                ReadOnlySpan<char> prettyName = default, name = default, version = default;
                foreach (string line in lines)
                {
                    ReadOnlySpan<char> lineSpan = line.AsSpan();

                    _ = TryGetFieldValue(lineSpan, "PRETTY_NAME=", ref prettyName) ||
                        TryGetFieldValue(lineSpan, "NAME=", ref name) ||
                        TryGetFieldValue(lineSpan, "VERSION=", ref version);

                    // Prefer "PRETTY_NAME".
                    if (!prettyName.IsEmpty)
                    {
                        return new string(prettyName);
                    }
                }

                // Fall back to "NAME[ VERSION]".
                if (!name.IsEmpty)
                {
                    if (!version.IsEmpty)
                    {
                        return string.Concat(name, " ", version);
                    }
                    return new string(name);
                }

                static bool TryGetFieldValue(ReadOnlySpan<char> line, ReadOnlySpan<char> prefix, ref ReadOnlySpan<char> value)
                {
                    if (!line.StartsWith(prefix))
                    {
                        return false;
                    }
                    ReadOnlySpan<char> fieldValue = line.Slice(prefix.Length);

                    // Remove enclosing quotes.
                    if (fieldValue.Length >= 2 &&
                        fieldValue[0] is '"' or '\'' &&
                        fieldValue[0] == fieldValue[^1])
                    {
                        fieldValue = fieldValue[1..^1];
                    }

                    value = fieldValue;
                    return true;
                }
            }

            return null;
        }
    }
}
