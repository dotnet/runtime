// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text
{
    internal static class NewLineUtility
    {
        public static int GetIndexOfFirstNewLineChar(ReadOnlySpan<char> span, out int charsToConsume)
        {
            // !! IMPORTANT !!
            //
            // We expect this method may be called with untrusted input, which means we need to bound the
            // worst-case runtime of this method. The logic below is specially written to run in worst-case
            // O(i) runtime, where i is the index of the first newline char in span. If there exists
            // no newline char in span, then the runtime is O(span.Length). The reason for this is that
            // we expect this method to be called in a loop and for the caller to slice, and this allows us
            // bound the entire loop in worst-case O(span.Length) runtime.
            //
            // Importantly, this means that we cannot call MemoryExtensions.IndexOfAny, as that method
            // is worst-case bounded by O(span.Length), not O(i). This means that when called in a loop,
            // the worst-case runtime for the loop is O(span.Length ^ 2), which could lead to an algorithmic
            // complexity attack.

            // The Unicode Standard, Sec. 5.8, Recommendation R4 and Table 5-2 state that the
            // following character sequences are considered newline chars:
            // - CR, LF, CRLF, NEL, LS, FF, and PS

            for (int i = 0; i < span.Length; i++)
            {
                switch (span[i])
                {
                    case '\n':     // LF
                    case '\f':     // FF
                    case '\u0085': // NEL
                    case '\u2028': // LS
                    case '\u2029': // PS
                        charsToConsume = 1;
                        return i;

                    case '\r':     // CR; consume any <LF> which occurs immediately afterward
                        int nextIdx = i + 1;
                        charsToConsume = ((uint)nextIdx < (uint)span.Length && span[nextIdx] == '\n') ? 2 : 1;
                        return i;
                }
            }

            charsToConsume = default;
            return -1; // EOF
        }
    }
}
