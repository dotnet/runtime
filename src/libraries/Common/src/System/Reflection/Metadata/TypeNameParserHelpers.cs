// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if SYSTEM_PRIVATE_CORELIB
#define NET8_0_OR_GREATER
#endif
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

#if SYSTEM_PRIVATE_CORELIB
using StringBuilder = System.Text.ValueStringBuilder;
#else
using StringBuilder = System.Text.StringBuilder;
#endif

using static System.Array;
using static System.Char;
using static System.Int32;

#nullable enable

namespace System.Reflection.Metadata
{
    internal static class TypeNameParserHelpers
    {
        internal const int SZArray = -1;
        internal const int Pointer = -2;
        internal const int ByRef = -3;
        private const char EscapeCharacter = '\\';
        private const string EndOfTypeNameDelimiters = ".+";
        private const string EndOfFullTypeNameDelimiters = "[]&*,+";
#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> _endOfTypeNameDelimitersSearchValues = SearchValues.Create(EndOfTypeNameDelimiters);
        private static readonly SearchValues<char> _endOfFullTypeNameDelimitersSearchValues = SearchValues.Create(EndOfFullTypeNameDelimiters);
#endif

        /// <returns>
        /// <para>Negative value for invalid type names.</para>
        /// <para>Zero for valid non-generic type names.</para>
        /// <para>Positive value for valid generic type names.</para>
        /// </returns>
        internal static int GetGenericArgumentCount(ReadOnlySpan<char> fullTypeName)
        {
            const int ShortestInvalidTypeName = 2; // Back tick and one digit. Example: "`1"
            if (fullTypeName.Length < ShortestInvalidTypeName || !IsAsciiDigit(fullTypeName[fullTypeName.Length - 1]))
            {
                return 0;
            }

            int backtickIndex = fullTypeName.Length - 2; // we already know it's true for the last one
            for (; backtickIndex >= 0; backtickIndex--)
            {
                if (fullTypeName[backtickIndex] == '`')
                {
                    if (backtickIndex == 0)
                    {
                        return -1; // illegal name, example "`1"
                    }
                    else if (fullTypeName[backtickIndex - 1] == EscapeCharacter)
                    {
                        return 0; // legal name, but not a generic type definition. Example: "Escaped\\`1"
                    }
                    else if (TryParse(fullTypeName.Slice(backtickIndex + 1), out int value))
                    {
                        // From C# 2.0 language spec: 8.16.3 Multiple type parameters Generic type declarations can have any number of type parameters.
                        if (value > MaxLength)
                        {
                            // But.. it's impossible to create a type with more than Array.MaxLength.
                            // OOM is also not welcomed in the parser!
                            return -1;
                        }

                        // the value can still be negative, but it's fine as the caller should treat that as an error
                        return value;
                    }

                    // most likely the value was too large to be parsed as an int
                    return -1;
                }
                else if (!IsAsciiDigit(fullTypeName[backtickIndex]) && fullTypeName[backtickIndex] != '-')
                {
                    break;
                }
            }

            return 0;
        }

        internal static string GetGenericTypeFullName(ReadOnlySpan<char> fullTypeName, TypeName[]? genericArgs)
        {
            if (genericArgs is null)
            {
                return fullTypeName.ToString();
            }

            int size = fullTypeName.Length + 1;
            foreach (TypeName genericArg in genericArgs)
            {
                size += 3 + genericArg.AssemblyQualifiedName.Length;
            }

            StringBuilder result = new(size);
#if NET8_0_OR_GREATER
            result.Append(fullTypeName);
#else
            for (int i = 0; i < fullTypeName.Length; i++)
            {
                result.Append(fullTypeName[i]);
            }
#endif
            result.Append('[');
            foreach (TypeName genericArg in genericArgs)
            {
                result.Append('[');
                result.Append(genericArg.AssemblyQualifiedName);
                result.Append(']');
                result.Append(',');
            }
            result[result.Length - 1] = ']'; // replace ',' with ']'

            return result.ToString();
        }

        // Normalizes "not found" to input length, since caller is expected to slice.
        internal static int GetFullTypeNameLength(ReadOnlySpan<char> input, out bool isNestedType)
        {
            // NET 6+ guarantees that MemoryExtensions.IndexOfAny has worst-case complexity
            // O(m * i) if a match is found, or O(m * n) if a match is not found, where:
            //   i := index of match position
            //   m := number of needles
            //   n := length of search space (haystack)
            //
            // Downlevel versions of .NET do not make this guarantee, instead having a
            // worst-case complexity of O(m * n) even if a match occurs at the beginning of
            // the search space. Since we're running this in a loop over untrusted user
            // input, that makes the total loop complexity potentially O(m * n^2), where
            // 'n' is adversary-controlled. To avoid DoS issues here, we'll loop manually.

#if NET8_0_OR_GREATER
            int offset = input.IndexOfAny(_endOfFullTypeNameDelimitersSearchValues);
#elif NET6_0_OR_GREATER
            int offset = input.IndexOfAny(EndOfTypeNameDelimiters);
#else
            int offset;
            for (offset = 0; offset < input.Length; offset++)
            {
                if (EndOfFullTypeNameDelimiters.IndexOf(input[offset]) >= 0) { break; }
            }
#endif
            isNestedType = offset > 0 && offset < input.Length && input[offset] == '+';

            return (int)Math.Min((uint)offset, (uint)input.Length);
        }

        internal static ReadOnlySpan<char> GetName(ReadOnlySpan<char> fullName)
        {
#if NET8_0_OR_GREATER
            int offset = fullName.LastIndexOfAny(_endOfTypeNameDelimitersSearchValues);
#elif NET6_0_OR_GREATER
            int offset = fullName.LastIndexOfAny(EndOfTypeNameDelimiters);
#else
            int offset = fullName.Length - 1;
            for (; offset >= 0; offset--)
            {
                if (EndOfTypeNameDelimiters.IndexOf(fullName[offset]) >= 0) { break; }
            }
#endif
            return offset < 0 ? fullName : fullName.Slice(offset + 1);
        }

        internal static string GetRankOrModifierStringRepresentation(int rankOrModifier)
        {
            return rankOrModifier switch
            {
                ByRef => "&",
                Pointer => "*",
                SZArray => "[]",
                1 => "[*]",
                _ => ArrayRankToString(rankOrModifier)
            };

            static string ArrayRankToString(int arrayRank)
            {
                Debug.Assert(arrayRank >= 2 && arrayRank <= 32);

#if NET8_0_OR_GREATER
                return string.Create(2 + arrayRank - 1, arrayRank, (buffer, rank) =>
                {
                    buffer[0] = '[';
                    for (int i = 1; i < rank; i++)
                        buffer[i] = ',';
                    buffer[^1] = ']';
                });
#else
                StringBuilder sb = new(2 + arrayRank - 1);
                sb.Append('[');
                for (int i = 1; i < arrayRank; i++)
                    sb.Append(',');
                sb.Append(']');
                return sb.ToString();
#endif
            }
        }

        /// <summary>
        /// Are there any captured generic args? We'll look for "[[" and "[" that is not followed by "]", "*" and ",".
        /// </summary>
        internal static bool IsBeginningOfGenericAgs(ref ReadOnlySpan<char> span, out bool doubleBrackets)
        {
            doubleBrackets = false;

            if (!span.IsEmpty && span[0] == '[')
            {
                // There are no spaces allowed before the first '[', but spaces are allowed after that.
                ReadOnlySpan<char> trimmed = TrimStart(span.Slice(1));
                if (!trimmed.IsEmpty)
                {
                    if (trimmed[0] == '[')
                    {
                        doubleBrackets = true;
                        span = TrimStart(trimmed.Slice(1));
                        return true;
                    }
                    if (!(trimmed[0] is ',' or '*' or ']')) // [] or [*] or [,] or [,,,, ...]
                    {
                        span = trimmed;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> input) => input.TrimStart();

        internal static bool TryGetTypeNameInfo(ReadOnlySpan<char> input, ref List<int>? nestedNameLengths,
            out int totalLength, out int genericArgCount)
        {
            bool isNestedType;
            totalLength = 0;
            genericArgCount = 0;
            do
            {
                int length = GetFullTypeNameLength(input.Slice(totalLength), out isNestedType);
                if (length <= 0) // it's possible only for a pair of unescaped '+' characters
                {
                    return false;
                }

                int generics = GetGenericArgumentCount(input.Slice(totalLength, length));
                if (generics < 0)
                {
                    return false; // invalid type name detected!
                }
                genericArgCount += generics;

                if (isNestedType)
                {
                    // do not validate the type name now, it will be validated as a whole nested type name later
                    (nestedNameLengths ??= new()).Add(length);
                    totalLength += 1; // skip the '+' sign in next search
                }
                totalLength += length;
            } while (isNestedType);

            return true;
        }

        internal static bool TryStripFirstCharAndTrailingSpaces(ref ReadOnlySpan<char> span, char value)
        {
            if (!span.IsEmpty && span[0] == value)
            {
                span = TrimStart(span.Slice(1));
                return true;
            }
            return false;
        }

#if !NETCOREAPP
        private const int MaxLength = 2147483591;

        private static bool TryParse(ReadOnlySpan<char> input, out int value) => int.TryParse(input.ToString(), out value);

        private static bool IsAsciiDigit(char ch) => ch >= '0' && ch <= '9';
#endif
    }
}
