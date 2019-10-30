// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    public unsafe partial class Utf8SpanTests
    {
        /// <summary>
        /// All <see cref="Rune"/>s, U+0000..U+D800 and U+E000..U+10FFFF.
        /// </summary>
        private static IEnumerable<Rune> AllRunes
        {
            get
            {
                for (uint i = 0; i < 0xD800; i++)
                {
                    yield return new Rune(i);
                }
                for (uint i = 0xE000; i <= 0x10FFFF; i++)
                {
                    yield return new Rune(i);
                }
            }
        }

        /// <summary>
        /// All <see cref="Rune"/>s where <see cref="Rune.IsWhiteSpace(Rune)"/> returns <see langword="true"/>.
        /// </summary>
        private static readonly Lazy<Rune[]> WhiteSpaceRunes = new Lazy<Rune[]>(() => AllRunes.Where(Rune.IsWhiteSpace).ToArray());

        public static IEnumerable<object[]> Trim_TestData()
        {
            string[] testData = new string[]
            {
                null, // null
                "", // empty
                "\0", // contains null character - shouldn't be trimmed
                "Hello", // simple non-whitespace ASCII data
                "\u0009Hello\u000d", // C0 whitespace characters
                "\u0009\u0008\u0009Hello\u000e\u000b", // C0 whitespace + non-whitespace characters
                " Hello! ", // simple space chars (plus !, since it's adjacent to U+0020 SPACE)
                "\u0085\u0084\u0086\u0085", // U+0085 NEXT LINE (NEL), surrounded by adjacent non-whitespace chars
            };

            foreach (string entry in testData)
            {
                yield return new object[] { entry };
            }

            // A string with every possible whitespace character, just to test the limits

            StringBuilder builder = new StringBuilder();
            foreach (Rune whitespaceRune in WhiteSpaceRunes.Value)
            {
                builder.Append(whitespaceRune);
            }
            builder.Append("xyz");
            foreach (Rune whitespaceRune in WhiteSpaceRunes.Value)
            {
                builder.Append(whitespaceRune);
            }

            yield return new object[] { builder.ToString() };
        }

        private static bool TryParseSearchTermAsChar(object searchTerm, out char parsed)
        {
            if (searchTerm is char ch)
            {
                parsed = ch;
                return true;
            }
            else if (searchTerm is Rune r)
            {
                if (r.IsBmp)
                {
                    parsed = (char)r.Value;
                    return true;
                }
            }
            else if (searchTerm is string str)
            {
                if (str.Length == 1)
                {
                    parsed = str[0];
                    return true;
                }
            }
            else if (searchTerm is ustring ustr)
            {
                var enumerator = ustr.Chars.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    parsed = enumerator.Current;
                    if (!enumerator.MoveNext())
                    {
                        return true;
                    }
                }
            }

            parsed = default; // failed to turn the search term into a single char
            return false;
        }

        private static bool TryParseSearchTermAsRune(object searchTerm, out Rune parsed)
        {
            if (searchTerm is char ch)
            {
                return Rune.TryCreate(ch, out parsed);
            }
            else if (searchTerm is Rune r)
            {
                parsed = r;
                return true;
            }
            else if (searchTerm is string str)
            {
                if (Rune.DecodeFromUtf16(str, out parsed, out int charsConsumed) == OperationStatus.Done
                    && charsConsumed == str.Length)
                {
                    return true;
                }
            }
            else if (searchTerm is ustring ustr)
            {
                if (Rune.DecodeFromUtf8(ustr.AsBytes(), out parsed, out int bytesConsumed) == OperationStatus.Done
                    && bytesConsumed == ustr.Length)
                {
                    return true;
                }
            }

            parsed = default; // failed to turn the search term into a single Rune
            return false;
        }

        private static bool TryParseSearchTermAsUtf8String(object searchTerm, out ustring parsed)
        {
            if (searchTerm is char ch)
            {
                if (Rune.TryCreate(ch, out Rune rune))
                {
                    parsed = rune.ToUtf8String();
                    return true;
                }
            }
            else if (searchTerm is Rune r)
            {
                parsed = r.ToUtf8String();
                return true;
            }
            else if (searchTerm is string str)
            {
                if (ustring.TryCreateFrom(str, out parsed))
                {
                    return true;
                }
            }
            else if (searchTerm is ustring ustr)
            {
                parsed = ustr;
                return true;
            }

            parsed = default; // failed to turn the search term into a ustring
            return false;
        }
    }
}
