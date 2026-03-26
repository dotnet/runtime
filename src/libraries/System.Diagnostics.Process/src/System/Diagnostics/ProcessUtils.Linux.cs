// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        /// <summary>Gets execution path</summary>
        internal static string? GetPathToOpenFile()
        {
            ReadOnlySpan<string> allowedProgramsToRun = ["xdg-open", "gnome-open", "kfmclient"];
            foreach (var program in allowedProgramsToRun)
            {
                string? pathToProgram = ProcessUtils.FindProgramInPath(program);
                if (!string.IsNullOrEmpty(pathToProgram))
                {
                    return pathToProgram;
                }
            }
            return null;
        }

    }
}
