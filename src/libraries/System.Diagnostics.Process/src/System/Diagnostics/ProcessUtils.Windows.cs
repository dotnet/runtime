// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        internal static void BuildArgs(string resolvedFilePath, IList<string>? arguments, ref ValueStringBuilder applicationName, ref ValueStringBuilder commandLine)
        {
            applicationName.Append(resolvedFilePath);
            applicationName.NullTerminate();

            // From: https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw
            // "Because argv[0] is the module name, C programmers generally repeat the module name as the first token in the command line."
            // The truth is that some programs REQUIRE it (example: findstr). That is why we repeat it.
            PasteArguments.AppendArgument(ref commandLine, resolvedFilePath);

            if (arguments is not null)
            {
                foreach (string argument in arguments)
                {
                    PasteArguments.AppendArgument(ref commandLine, argument);
                }
            }
            commandLine.NullTerminate();
        }

        internal static string GetEnvironmentVariablesBlock(IDictionary<string, string?> sd)
        {
            // https://learn.microsoft.com/windows/win32/procthread/changing-environment-variables
            // "All strings in the environment block must be sorted alphabetically by name. The sort is
            //  case-insensitive, Unicode order, without regard to locale. Because the equal sign is a
            //  separator, it must not be used in the name of an environment variable."

            var keys = new string[sd.Count];
            sd.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.OrdinalIgnoreCase);

            // Join the null-terminated "key=val\0" strings
            var result = new StringBuilder(8 * keys.Length);
            foreach (string key in keys)
            {
                string? value = sd[key];

                // Ignore null values for consistency with Environment.SetEnvironmentVariable
                if (value != null)
                {
                    result.Append(key).Append('=').Append(value).Append('\0');
                }
            }

            return result.ToString();
        }

        private static bool IsExecutable(string fullPath)
        {
            return File.Exists(fullPath);
        }
    }
}
