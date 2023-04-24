// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class ArgumentEscaper
    {
        /// <summary>
        /// Undo the processing which took place to create string[] args in Main,
        /// so that the next process will receive the same string[] args
        ///
        /// See here for more info:
        /// https://docs.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string EscapeAndConcatenateArgArrayForProcessStart(IEnumerable<string> args)
        {
            return string.Join(" ", EscapeArgArray(args));
        }

        /// <summary>
        /// Undo the processing which took place to create string[] args in Main,
        /// so that the next process will receive the same string[] args
        ///
        /// See here for more info:
        /// https://docs.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IEnumerable<string> EscapeArgArray(IEnumerable<string> args)
        {
            var escapedArgs = new List<string>();

            foreach (var arg in args)
            {
                escapedArgs.Add(EscapeArg(arg));
            }

            return escapedArgs;
        }

        private static string EscapeArg(string arg)
        {
            var sb = new StringBuilder();

            var quoted = ShouldSurroundWithQuotes(arg);
            if (quoted) sb.Append('"');

            for (int i = 0; i < arg.Length; ++i)
            {
                var backslashCount = 0;

                // Consume All Backslashes
                while (i < arg.Length && arg[i] == '\\')
                {
                    backslashCount++;
                    i++;
                }

                // Escape any backslashes at the end of the arg
                // This ensures the outside quote is interpreted as
                // an argument delimiter
                if (i == arg.Length)
                {
                    sb.Append('\\', 2 * backslashCount);
                }

                // Escape any preceding backslashes and the quote
                else if (arg[i] == '"')
                {
                    sb.Append('\\', (2 * backslashCount) + 1);
                    sb.Append('"');
                }

                // Output any consumed backslashes and the character
                else
                {
                    sb.Append('\\', backslashCount);
                    sb.Append(arg[i]);
                }
            }

            if (quoted) sb.Append('"');

            return sb.ToString();
        }

        /// <summary>
        /// Prepare as single argument to
        /// roundtrip properly through cmd.
        ///
        /// This prefixes every character with the '^' character to force cmd to
        /// interpret the argument string literally. An alternative option would
        /// be to do this only for cmd metacharacters.
        ///
        /// See here for more info:
        /// https://docs.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static bool ShouldSurroundWithQuotes(string argument)
        {
            // Don't quote already quoted strings
            if (argument.StartsWith("\"", StringComparison.Ordinal) &&
                    argument.EndsWith("\"", StringComparison.Ordinal))
            {
                return false;
            }

            // Only quote if whitespace exists in the string
            if (argument.Contains(" ") || argument.Contains("\t") || argument.Contains("\n"))
            {
                return true;
            }

            return false;
        }
    }
}
