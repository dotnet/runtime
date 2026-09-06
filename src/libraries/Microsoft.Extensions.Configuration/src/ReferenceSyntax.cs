// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    // Lexical helpers for the reference syntax.
    internal static class ReferenceSyntax
    {
        internal const char KeyDelimiter = ':';
        internal const char SelfMarker = '.';
        internal const char BraceOpen = '{';
        internal const char BraceClose = '}';

        private const char Sigil = '$';
        private const string Keyword = "ref(";
        private const char BodyClose = ')';

        // The shortest a reference can be, "$ref()", which is also what makes the keyword comparison below safe.
        private const int MinLength = 6;

        // Whether <paramref name="value"/> is a reference, and if so the span between the opening parenthesis and the
        // final ')'. The keyword is matched case-insensitively, in keeping with configuration keys generally. There is
        // no escape: a value that spells a reference is one, and an application holding text that cannot be read any
        // other way turns transformations off rather than learning a syntax to say so.
        internal static bool IsReference(string value, out int bodyStart, out int bodyLength)
        {
            bodyStart = 0;
            bodyLength = 0;

            if (value.Length < MinLength
             || value[value.Length - 1] != BodyClose
             || value[0] != Sigil
             || string.Compare(value, 1, Keyword, 0, Keyword.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            bodyStart = 1 + Keyword.Length;
            bodyLength = value.Length - 1 - bodyStart;
            return true;
        }

        // Whether <paramref name="c"/> opens a quoted run. Either quote character will do, so a key containing one is
        // written in the other.
        internal static bool IsQuote(char c) => c == '\'' || c == '"';

        // Advances past a quoted run that starts at <paramref name="start"/>, returning the index just past its closing
        // quote, or -1 when the run is never closed. A doubled quote is content, not the terminator.
        internal static int SkipQuoted(ReadOnlySpan<char> s, int start)
        {
            char quote = s[start];
            for (int i = start + 1; i < s.Length; i++)
            {
                if (s[i] == quote)
                {
                    if (i + 1 < s.Length && s[i + 1] == quote)
                    {
                        i++;
                        continue;
                    }

                    return i + 1;
                }
            }

            return -1;
        }

        // Writes the content of the quoted run spanning [start, pastEnd) - as returned by <see cref="SkipQuoted"/> -
        // at <paramref name="write"/>, with the surrounding quotes removed and each doubled quote collapsed to one,
        // and returns where the key now ends. Reading and writing share the buffer, which is safe because the run is
        // always written shorter than it is read.
        internal static int WriteQuoted(ref ValueStringBuilder text, int start, int pastEnd, int write)
        {
            char quote = text[start];
            int last = pastEnd - 2;
            for (int i = start + 1; i <= last; i++)
            {
                char c = text[i];
                text[write++] = c;
                if (c == quote)
                {
                    i++;
                }
            }

            return write;
        }

        // Finds the brace closing the one at <paramref name="open"/>, skipping quoted runs, and throws if the text ends
        // first. What a placeholder names is a key expression with no placeholder of its own, so a second opening brace
        // is a mistake rather than a level to descend into.
        internal static int FindMatchingBrace(ReadOnlySpan<char> s, int open, string key)
        {
            int i = open + 1;
            while (i < s.Length)
            {
                char c = s[i];
                if (IsQuote(c))
                {
                    i = SkipQuoted(s, i);
                    if (i < 0)
                    {
                        break;
                    }

                    continue;
                }

                if (c == BraceClose)
                {
                    return i;
                }

                if (c == BraceOpen)
                {
                    throw new InvalidOperationException(SR.Format(SR.Error_NestedSubReference, key));
                }

                i++;
            }

            throw new InvalidOperationException(SR.Format(SR.Error_MalformedReference, key));
        }

        // The length of the move at <paramref name="i"/>, or 0 when the dots there are part of a name rather than a
        // move. A move has to fill a whole segment, so it starts one and ends one. What it starts from is the key built
        // so far, of which <paramref name="written"/> characters stand at the head of <paramref name="s"/>, rather than
        // the text they were read from, which may have held quotes that were dropped on the way.
        internal static int MoveLength(ReadOnlySpan<char> s, int i, int written)
        {
            if (s[i] != SelfMarker || (written > 0 && s[written - 1] != KeyDelimiter))
            {
                return 0;
            }

            int past = i;
            while (past < s.Length && s[past] == SelfMarker)
            {
                past++;
            }

            int dots = past - i;
            return dots <= 2 && (past == s.Length || s[past] == KeyDelimiter) ? dots : 0;
        }
    }
}
