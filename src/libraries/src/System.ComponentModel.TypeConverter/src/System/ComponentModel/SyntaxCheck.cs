// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <internalonly/>
    /// <summary>
    ///     SyntaxCheck
    ///     Helper class to check for path and machine name syntax.
    /// </summary>
    public static class SyntaxCheck
    {
        /// <summary>
        ///     Checks the syntax of the machine name (no "\" anywhere in it).
        /// </summary>
        /// <internalonly/>
        public static bool CheckMachineName(string value)
        {
            if (value == null)
                return false;

            value = value.Trim();
            if (value.Equals(String.Empty))
                return false;

            // Machine names shouldn't contain any "\"
            return (value.IndexOf('\\') == -1);
        }

        /// <summary>
        ///     Checks the syntax of the path (must start with "\\").
        /// </summary>
        /// <internalonly/>
        public static bool CheckPath(string value)
        {
            if (value == null)
                return false;

            value = value.Trim();
            if (value.Equals(String.Empty))
                return false;

            // Path names should start with "\\"
            return value.StartsWith("\\\\");
        }

        /// <summary>
        ///     Checks the syntax of the path (must start with "\" or drive letter "C:").
        ///     NOTE:  These denote a file or directory path!!
        ///     
        /// </summary>
        /// <internalonly/>
        public static bool CheckRootedPath(string value)
        {
            if (value == null)
                return false;

            value = value.Trim();
            if (value.Equals(String.Empty))
                return false;

            // Is it rooted?
            return Path.IsPathRooted(value);
        }
    }
}
