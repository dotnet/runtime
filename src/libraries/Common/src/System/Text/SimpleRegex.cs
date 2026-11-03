// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text
{
    internal static class SimpleRegex
    {
        // Based on wildcmp written by Jack Handy - <A href="mailto:jakkhandy@hotmail.com">jakkhandy@hotmail.com</A>
        // https://www.codeproject.com/Articles/1088/Wildcard-string-compare-globbing

        /// <summary>
        /// Perform a match between an input string and a pattern in which the only special character
        /// is an asterisk, which can map to zero or more of any character in the input.
        /// </summary>
        /// <param name="input">The input to match.</param>
        /// <param name="pattern">The pattern to match against.</param>
        /// <returns>true if the input matches the pattern; otherwise, false.</returns>
        public static bool IsMatchWithStarWildcard(ReadOnlySpan<char> input, ReadOnlySpan<char> pattern)
        {
            int inputPos = 0, inputPosSaved = -1;
            int patternPos = 0, patternPosSaved = -1;

            // Loop through each character in the input.
            while (inputPos < input.Length)
            {
                if (patternPos < pattern.Length && pattern[patternPos] == '*')
                {
                    // If we're currently positioned on a wildcard in the pattern,
                    // move past it and remember where we are to backtrack to.
                    inputPosSaved = inputPos;
                    patternPosSaved = ++patternPos;
                }
                else if (patternPos < pattern.Length &&
                    (pattern[patternPos] == input[inputPos] ||
                     char.ToUpperInvariant(pattern[patternPos]) == char.ToUpperInvariant(input[inputPos])))
                {
                    // If the characters in the pattern and the input match, advance both.
                    inputPos++;
                    patternPos++;
                }
                else if (patternPosSaved == -1)
                {
                    // If we're not on a wildcard and the current characters don't match and we don't have
                    // any wildcard to backtrack to, this is not a match.
                    return false;
                }
                else
                {
                    // Otherwise, this is not a wildcard, the characters don't match, but we do have a
                    // wildcard saved, so backtrack to it and use it to consume the next input character.
                    inputPos = ++inputPosSaved;
                    patternPos = patternPosSaved;
                }
            }

            // Any remaining pattern characters must be wildcards that match empty input.
            Debug.Assert(patternPos <= pattern.Length);
            return !pattern.Slice(patternPos).ContainsAnyExcept('*');
        }
    }
}
