// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System
{
    internal static partial class PasteArguments
    {
        /// <summary>
        /// Repastes a set of arguments into a linear string that parses back into the originals under pre- or post-2008 VC parsing rules.
        /// The rules for parsing the executable name (argv[0]) are special, so you must indicate whether the first argument actually is argv[0].
        /// </summary>
        internal static string Paste(IEnumerable<string> arguments, bool pasteFirstArgumentUsingArgV0Rules)
        {
            var stringBuilder = new ValueStringBuilder(stackalloc char[256]);

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
                            throw new ApplicationException(SR.Argv_IncludeDoubleQuote);
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
                    AppendArgument(ref stringBuilder, argument);
                }
            }

            return stringBuilder.ToString();
        }
    }
}
