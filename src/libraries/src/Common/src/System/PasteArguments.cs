// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;

namespace System
{
    internal static class PasteArguments
    {
         /// <summary>
        /// Repastes a set of arguments into a linear string that parses back into the originals under pre- or post-2008 VC parsing rules.
        /// The rules for parsing the executable name (argv[0]) are special, so you must indicate whether the first argument actually is argv[0].
        /// </summary>
        public static string Paste(IEnumerable<string> arguments, bool pasteFirstArgumentUsingArgV0Rules)
        {
            var stringBuilder = new StringBuilder();

            foreach (string argument in arguments)
            {
                if (pasteFirstArgumentUsingArgV0Rules)
                {
                    pasteFirstArgumentUsingArgV0Rules = false;

                    // Special rules for argv[0]
                    //   - Backslash is a normal character.
                    //   - Quotes used to include whitespace characters.
                    //   - Parsing ends at first whitespace outside quoted region.
                    //   - No way to get a literal quote past the parser.

                    bool hasWhitespace = false;
                    foreach (char c in argument)
                    {
                        if (c == Quote)
                        {
                            throw new ApplicationException("The argv[0] argument cannot include a double quote.");
                        }
                        if (char.IsWhiteSpace(c))
                        {
                            hasWhitespace = true;
                        }
                    }
                    if (argument.Length == 0 || hasWhitespace)
                    {
                        stringBuilder.Append(Quote);
                        stringBuilder.Append(argument);
                        stringBuilder.Append(Quote);
                    }
                    else
                    {
                        stringBuilder.Append(argument);
                    }
                }
                else
                {
                    if (stringBuilder.Length != 0)
                    {
                        stringBuilder.Append(' ');
                    }

                    // Parsing rules for non-argv[0] arguments:
                    //   - Backslash is a normal character except followed by a quote.
                    //   - 2N backslashes followed by a quote ==> N literal backslashes followed by unescaped quote
                    //   - 2N+1 backslashes followed by a quote ==> N literal backslashes followed by a literal quote
                    //   - Parsing stops at first whitespace outside of quoted region.
                    //   - (post 2008 rule): A closing quote followed by another quote ==> literal quote, and parsing remains in quoting mode.
                    if (argument.Length != 0 && ContainsNoWhitespaceOrQuotes(argument))
                    {
                        // Simple case - no quoting or changes needed.
                        stringBuilder.Append(argument);
                    }
                    else
                    {
                        stringBuilder.Append(Quote);
                        int idx = 0;
                        while (idx < argument.Length)
                        {
                            char c = argument[idx++];
                            if (c == Backslash)
                            {
                                int numBackSlash = 1;
                                while (idx < argument.Length && argument[idx] == Backslash)
                                {
                                    idx++;
                                    numBackSlash++;
                                }
                                if (idx == argument.Length)
                                {
                                    // We'll emit an end quote after this so must double the number of backslashes.
                                    stringBuilder.Append(Backslash, numBackSlash * 2);
                                }
                                else if (argument[idx] == Quote)
                                {
                                    // Backslashes will be followed by a quote. Must double the number of backslashes.
                                    stringBuilder.Append(Backslash, numBackSlash * 2 + 1);
                                    stringBuilder.Append(Quote);
                                    idx++;
                                }
                                else
                                {
                                    // Backslash will not be followed by a quote, so emit as normal characters.
                                    stringBuilder.Append(Backslash, numBackSlash);
                                }
                                continue;
                            }
                            if (c == Quote)
                            {
                                // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                                // by another quote (which parses differently pre-2008 vs. post-2008.)
                                stringBuilder.Append(Backslash);
                                stringBuilder.Append(Quote);
                                continue;
                            }
                            stringBuilder.Append(c);
                        }
                        stringBuilder.Append(Quote);
                    }
                }
            }

            return stringBuilder.ToString();
        }

        private static bool ContainsNoWhitespaceOrQuotes(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c) || c == Quote)
                {
                    return false;
                }
            }

            return true;
        }

        private const char Quote = '\"';
        private const char Backslash = '\\';
    }
}
