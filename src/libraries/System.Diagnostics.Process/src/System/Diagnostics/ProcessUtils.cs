// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Threading;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        internal static readonly ReaderWriterLockSlim s_processStartLock = new ReaderWriterLockSlim();
        internal static int s_cachedSerializationSwitch;

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
                    // On Unix, we need to verify the file has execute permissions.
                    // On Windows, any file that exists is considered executable.
                    if (IsExecutable(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        internal static Win32Exception CreateExceptionForErrorStartingProcess(string errorMessage, int errorCode, string fileName, string? workingDirectory)
        {
            string directoryForException = string.IsNullOrEmpty(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory;
            string msg = SR.Format(SR.ErrorStartingProcess, fileName, directoryForException, errorMessage);
            return new Win32Exception(errorCode, msg);
        }
    }
}
