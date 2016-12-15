// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Configuration
{
    internal static class ConfigPathUtility
    {
        private const char SeparatorChar = '/';

        // A configPath is valid if
        //  * It does not start or end with a '/'
        //  * It is not null or empty, except in the case of the root configuration record
        //  * It does not contain '\'
        //  * It does not contain a path component equal to "." or ".."
        //
        // The checks for '\', ".", and ".." are not strictly necessary, but their presence
        // could lead to problems for configuration hosts.
        internal static bool IsValid(string configPath)
        {
            if (string.IsNullOrEmpty(configPath))
                return false;

            int start = -1;
            for (int examine = 0; examine <= configPath.Length; examine++)
            {
                char ch;

                ch = examine < configPath.Length ? configPath[examine] : SeparatorChar;

                // backslash disallowed
                if (ch == '\\')
                    return false;

                if (ch == SeparatorChar)
                {
                    // double slash disallowed
                    // note this check also purposefully catches starting and ending slash
                    if (examine == start + 1)
                        return false;

                    // "." disallowed
                    if ((examine == start + 2) && (configPath[start + 1] == '.'))
                        return false;

                    // ".." disallowed
                    if ((examine == start + 3) && (configPath[start + 1] == '.') && (configPath[start + 2] == '.'))
                        return false;

                    start = examine;
                }
            }

            return true;
        }

        internal static string Combine(string parentConfigPath, string childConfigPath)
        {
            Debug.Assert(string.IsNullOrEmpty(parentConfigPath) || IsValid(parentConfigPath),
                "String.IsNullOrEmpty(parentConfigPath) || IsValid(parentConfigPath)");
            Debug.Assert(string.IsNullOrEmpty(childConfigPath) || IsValid(childConfigPath),
                "String.IsNullOrEmpty(childConfigPath) || IsValid(childConfigPath)");

            if (string.IsNullOrEmpty(parentConfigPath))
                return childConfigPath;

            if (string.IsNullOrEmpty(childConfigPath))
                return parentConfigPath;

            return parentConfigPath + SeparatorChar + childConfigPath;
        }

        internal static string[] GetParts(string configPath)
        {
            Debug.Assert(IsValid(configPath), "IsValid(configPath)");

            string[] parts = configPath.Split(SeparatorChar);
            return parts;
        }

        // Return the last part of a config path, e.g.
        //   GetName("MACHINE/WEBROOT/Default Web Site/app") == "app"
        internal static string GetName(string configPath)
        {
            Debug.Assert(string.IsNullOrEmpty(configPath) || IsValid(configPath),
                "String.IsNullOrEmpty(configPath) || IsValid(configPath)");

            if (string.IsNullOrEmpty(configPath))
                return configPath;

            int index = configPath.LastIndexOf(SeparatorChar);
            if (index == -1)
                return configPath;

            Debug.Assert(index != configPath.Length - 1);
            return configPath.Substring(index + 1);
        }
    }
}