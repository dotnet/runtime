// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        /// <summary>Gets execution path</summary>
        internal static string? GetPathToOpenFile()
        {
            return ProcessUtils.FindProgramInPath("xdg-open");
        }

    }
}
