// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Web.Util
{
    internal static class Utf16StringValidator
    {
        private const char UnicodeReplacementChar = '\uFFFD';

        internal static string ValidateString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // locate the first surrogate character
            int idxOfFirstSurrogate = -1;
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsSurrogate(input[i]))
                {
                    idxOfFirstSurrogate = i;
                    break;
                }
            }

            // fast case: no surrogates = return input string
            if (idxOfFirstSurrogate < 0)
            {
                return input;
            }

            // slow case: surrogates exist, so we need to validate them
            return string.Create(input.Length, (input, idxOfFirstSurrogate), (chars, state) =>
            {
                state.input.CopyTo(chars);
                for (int i = state.idxOfFirstSurrogate; i < chars.Length; i++)
                {
                    char thisChar = chars[i];

                    // If this character is a low surrogate, then it was not preceded by
                    // a high surrogate, so we'll replace it.
                    if (char.IsLowSurrogate(thisChar))
                    {
                        chars[i] = UnicodeReplacementChar;
                        continue;
                    }

                    if (char.IsHighSurrogate(thisChar))
                    {
                        // If this character is a high surrogate and it is followed by a
                        // low surrogate, allow both to remain.
                        if (i + 1 < chars.Length && char.IsLowSurrogate(chars[i + 1]))
                        {
                            i++; // skip the low surrogate also
                            continue;
                        }

                        // If this character is a high surrogate and it is not followed
                        // by a low surrogate, replace it.
                        chars[i] = UnicodeReplacementChar;
                        continue;
                    }

                    // Otherwise, this is a non-surrogate character and just move to the
                    // next character.
                }
            });
        }
    }
}
