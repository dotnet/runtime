// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.ComponentModel
{
    /// <summary>
    /// SyntaxCheck
    /// Helper class to check for path and machine name syntax.
    /// </summary>
    public static class SyntaxCheck
    {
        /// <summary>
        /// Checks the syntax of the machine name (no "\" anywhere in it).
        /// </summary>
        public static bool CheckMachineName(string value)
        {
            if (value == null)
                return false;

            value = value.Trim();
            if (value.Equals(string.Empty))
                return false;

            // Machine names shouldn't contain any "\"
            return !value.Contains('\\');
        }

        /// <summary>
        /// Checks the syntax of the path (must start with "\\").
        /// </summary>
        public static bool CheckPath(string value)
        {
            if (value == null)
                return false;

            value = value.Trim();
            if (value.Equals(string.Empty))
                return false;

            // Path names should start with "\\"
            return value.StartsWith("\\\\");
        }

        /// <summary>
        /// Checks the syntax of the path (must start with "\" or drive letter "C:").
        /// NOTE:  These denote a file or directory path!!
        /// </summary>
        public static bool CheckRootedPath(string value)
        {
            if (value == null)
                return false;

            value = value.Trim();
            if (value.Equals(string.Empty))
                return false;

            // Is it rooted?
            return Path.IsPathRooted(value);
        }
    }
}
