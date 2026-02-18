// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        internal static void BuildArgs(ProcessStartOptions options, ref ValueStringBuilder applicationName, ref ValueStringBuilder commandLine)
        {
            string absolutePath = options.FileName;

            applicationName.Append(absolutePath);
            applicationName.NullTerminate();

            // From: https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw
            // "Because argv[0] is the module name, C programmers generally repeat the module name as the first token in the command line."
            // The truth is that some programs REQUIRE it (example: findstr). That is why we repeat it.
            PasteArguments.AppendArgument(ref commandLine, options.FileName);

            foreach (string argument in options.Arguments)
            {
                PasteArguments.AppendArgument(ref commandLine, argument);
            }
            commandLine.NullTerminate();
        }

        private static bool IsExecutable(string fullPath)
        {
            return File.Exists(fullPath);
        }
    }
}
