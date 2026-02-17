// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        private static bool IsExecutable(string fullPath)
        {
            return File.Exists(fullPath);
        }

        internal static string? FindProgramInPath(string program)
        {
            string? pathEnvVar = System.Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar is not null)
            {
                StringParser pathParser = new(pathEnvVar, Path.PathSeparator, skipEmpty: true);
                while (pathParser.MoveNext())
                {
                    string subPath = pathParser.ExtractCurrent();
                    string path = Path.Combine(subPath, program);
                    if (IsExecutable(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }
    }
}
