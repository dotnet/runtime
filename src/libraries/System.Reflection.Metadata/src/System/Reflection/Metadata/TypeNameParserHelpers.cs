// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Metadata
{
    internal static class TypeNameParserHelpers
    {
        internal const int SZArray = -1;
        internal const int Pointer = -2;
        internal const int ByRef = -3;
        private const char EscapeCharacter = '\\';
#if NET8_0_OR_GREATER
        // Keep this in sync with GetFullTypeNameLength/NeedsEscaping
        private static readonly SearchValues<char> s_endOfFullTypeNameDelimitersSearchValues = SearchValues.Create("[]&*,+\\");
#endif
        /// <returns>Positive length or negative value for invalid name</returns>
        internal static int GetFullTypeNameLength(ReadOnlySpan<char> input, out bool isNestedType)
        {
            isNestedType = false;

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
            int offset = input.IndexOfAny(s_endOfFullTypeNameDelimitersSearchValues);
            if (offset < 0)
            {
                return input.Length; // no type name end chars were found, the whole input is the type name
            }

            if (input[offset] == EscapeCharacter) // this is very rare (IL Emit or pure IL)
            {
                offset = GetUnescapedOffset(input, startOffset: offset); // this is slower, but very rare so acceptable
            }
#else
            int offset = GetUnescapedOffset(input, startOffset: 0);
#endif
            isNestedType = offset > 0 && offset < input.Length && input[offset] == '+';
            return offset;

            static int GetUnescapedOffset(ReadOnlySpan<char> input, int startOffset)
            {
                int offset = startOffset;
                for (; offset < input.Length; offset++)
                {
                    char c = input[offset];
                    if (c == EscapeCharacter)
                    {
                        offset++; // skip the escaped char

                        if (offset == input.Length || // invalid name that ends with escape character
                            !NeedsEscaping(input[offset])) // invalid name, escapes a char that does not need escaping
                        {
                            return -1;
                        }
                    }
                    else if (NeedsEscaping(c))
                    {
                        break;
                    }
                }
                return offset;
            }

            // Keep this in sync with s_endOfFullTypeNameDelimitersSearchValues
            static bool NeedsEscaping(char c) => c is '[' or ']' or '&' or '*' or ',' or '+' or EscapeCharacter;
        }

        internal static int IndexOfNamespaceDelimiter(ReadOnlySpan<char> fullName)
        {
            // Matches algorithm from ns::FindSep in src\coreclr\utilcode\namespaceutil.cpp
            // This could result in the type name beginning with a '.' character.
            int index = fullName.LastIndexOf('.');

            if (index > 0 && fullName[index - 1] == '.')
            {
                index--;
            }

            return index;
        }

        internal static string Unescape(string input)
        {
            int indexOfEscapeCharacter = input.IndexOf(EscapeCharacter);
            if (indexOfEscapeCharacter < 0)
            {
                // Nothing to escape, just return the original value.
                return input;
            }

            return UnescapeToBuilder(input, indexOfEscapeCharacter);

            static string UnescapeToBuilder(string name, int indexOfEscapeCharacter)
            {
                // This code path is executed very rarely (IL Emit or pure IL with chars not allowed in C# or F#).
                var sb = new ValueStringBuilder(stackalloc char[64]);
                sb.EnsureCapacity(name.Length);
                sb.Append(name.AsSpan(0, indexOfEscapeCharacter));

                for (int i = indexOfEscapeCharacter; i < name.Length;)
                {
                    char c = name[i++];

                    if (c != EscapeCharacter || i == name.Length)
                    {
                        sb.Append(c);
                    }
                    else if (name[i] == EscapeCharacter) // escaped escape character ;)
                    {
                        sb.Append(c);
                        // Consume the escaped escape character, it's important for edge cases
                        // like escaped escape character followed by another escaped char (example: "\\\\\\+")
                        i++;
                    }
                }

                return sb.ToString();
            }
        }

        // this method handles escaping of the ] just to let the AssemblyNameParser fail for the right input
        internal static ReadOnlySpan<char> GetAssemblyNameCandidate(ReadOnlySpan<char> input)
        {
            // The only delimiter which can terminate an assembly name is ']'.
            // Otherwise EOL serves as the terminator.
            int offset = input.IndexOf(']');

            if (offset > 0 && input[offset - 1] == EscapeCharacter) // this should be very rare (IL Emit & pure IL)
            {
                offset = GetUnescapedOffset(input, startIndex: offset);
            }

            return offset < 0 ? input : input.Slice(0, offset);

            static int GetUnescapedOffset(ReadOnlySpan<char> input, int startIndex)
            {
                int offset = startIndex;
                for (; offset < input.Length; offset++)
                {
                    if (input[offset] is ']')
                    {
                        if (input[offset - 1] != EscapeCharacter)
                        {
                            break;
                        }
                    }
                }
                return offset;
            }
        }

        internal static void AppendRankOrModifierStringRepresentation(int rankOrModifier, ref ValueStringBuilder builder)
        {
            if (rankOrModifier == ByRef)
            {
                builder.Append('&');
            }
            else if (rankOrModifier == Pointer)
            {
                builder.Append('*');
            }
            else if (rankOrModifier == SZArray)
            {
                builder.Append("[]");
            }
            else if (rankOrModifier == 1)
            {
                builder.Append("[*]");
            }
            else
            {
                Debug.Assert(rankOrModifier >= 2);

                // O(rank) work, so we have to assume the rank is trusted. We don't put a hard cap on this,
                // but within the TypeName parser, we do require the input string to contain the correct number
                // of commas. This forces the input string to have at least O(rank) length, so there's no
                // alg. complexity attack possible here. Callers can of course pass any arbitrary value to
                // TypeName.MakeArrayTypeName, but per first sentence in this comment, we have to assume any
                // such arbitrary value which is programmatically fed in originates from a trustworthy source.

                builder.Append('[');
                builder.Append(',', rankOrModifier - 1);
                builder.Append(']');
            }
        }

        /// <summary>
        /// Are there any captured generic args? We'll look for "[[" and "[" that is not followed by "]", "*" and ",".
        /// </summary>
        internal static bool IsBeginningOfGenericArgs(ref ReadOnlySpan<char> span, out bool doubleBrackets)
        {
            doubleBrackets = false;

            if (!span.IsEmpty && span[0] == '[')
            {
                // There are no spaces allowed before the first '[', but spaces are allowed after that.
                ReadOnlySpan<char> trimmed = span.Slice(1).TrimStart();
                if (!trimmed.IsEmpty)
                {
                    if (trimmed[0] == '[')
                    {
                        doubleBrackets = true;
                        span = trimmed.Slice(1).TrimStart();
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

        internal static bool TryGetTypeNameInfo(TypeNameParseOptions options, ref ReadOnlySpan<char> input,
            ref List<int>? nestedNameLengths, ref int recursiveDepth, out int totalLength)
        {
            bool isNestedType;
            totalLength = 0;
            do
            {
                int length = GetFullTypeNameLength(input.Slice(totalLength), out isNestedType);
                if (length <= 0)
                {
                    // invalid type names:
                    // -1: invalid escaping
                    // 0: pair of unescaped "++" characters
                    return false;
                }

#if SYSTEM_PRIVATE_CORELIB
                // Compat: Ignore leading '.' for type names without namespace. .NET Framework historically ignored leading '.' here. It is likely
                // that code out there depends on this behavior. For example, type names formed by concatenating namespace and name, without checking for
                // empty namespace (bug), are going to have superfluous leading '.'.
                // This behavior means that types that start with '.' are not round-trippable via type name.
                if (length > 1 && input[0] == '.' && input.Slice(0, length).LastIndexOf('.') == 0)
                {
                    input = input.Slice(1);
                    length--;
                }
#endif
                if (isNestedType)
                {
                    if (!TryDive(options, ref recursiveDepth))
                    {
                        return false;
                    }

                    (nestedNameLengths ??= new()).Add(length);
                    totalLength += 1; // skip the '+' sign in next search
                }
                totalLength += length;
            } while (isNestedType);

            return true;
        }

        internal static bool TryParseNextDecorator(ref ReadOnlySpan<char> input, out int rankOrModifier)
        {
            // Then try pulling a single decorator.
            // Whitespace cannot precede the decorator, but it can follow the decorator.

            ReadOnlySpan<char> originalInput = input; // so we can restore on 'false' return

            if (TryStripFirstCharAndTrailingSpaces(ref input, '*'))
            {
                rankOrModifier = Pointer;
                return true;
            }

            if (TryStripFirstCharAndTrailingSpaces(ref input, '&'))
            {
                rankOrModifier = ByRef;
                return true;
            }

            if (TryStripFirstCharAndTrailingSpaces(ref input, '['))
            {
                // SZArray := []
                // MDArray := [*] or [,] or [,,,, ...]

                int rank = 1;
                bool hasSeenAsterisk = false;

            ReadNextArrayToken:

                if (TryStripFirstCharAndTrailingSpaces(ref input, ']'))
                {
                    // End of array marker
                    rankOrModifier = rank == 1 && !hasSeenAsterisk ? SZArray : rank;
                    return true;
                }

                if (!hasSeenAsterisk)
                {
                    if (rank == 1 && TryStripFirstCharAndTrailingSpaces(ref input, '*'))
                    {
                        // [*]
                        hasSeenAsterisk = true;
                        goto ReadNextArrayToken;
                    }
                    else if (TryStripFirstCharAndTrailingSpaces(ref input, ','))
                    {
                        // [,,, ...]
                        // The runtime restricts arrays to rank 32, but we don't enforce that here.
                        // Instead, the max rank is controlled by the total number of commas present
                        // in the array decorator.
                        checked { rank++; }
                        goto ReadNextArrayToken;
                    }
                }

                // Don't know what this token is.
                // Fall through to 'return false' statement.
            }

            input = originalInput; // ensure 'ref input' not mutated
            rankOrModifier = 0;
            return false;
        }

        internal static bool TryStripFirstCharAndTrailingSpaces(ref ReadOnlySpan<char> span, char value)
        {
            if (!span.IsEmpty && span[0] == value)
            {
                span = span.Slice(1).TrimStart();
                return true;
            }
            return false;
        }

        [DoesNotReturn]
        internal static void ThrowArgumentNullException(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        [DoesNotReturn]
        internal static void ThrowArgumentException_InvalidTypeName(int errorIndex)
        {
            throw new ArgumentException(SR.Argument_InvalidTypeName, $"typeName@{errorIndex}");
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperation_MaxNodesExceeded(int limit)
        {
#if SYSTEM_REFLECTION_METADATA
            throw new InvalidOperationException(SR.Format(SR.InvalidOperation_MaxNodesExceeded, limit));
#else
            Debug.Fail("Expected to be unreachable");
            throw new InvalidOperationException();
#endif
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperation_NotGenericType()
        {
#if SYSTEM_REFLECTION_METADATA
            throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
#else
            Debug.Fail("Expected to be unreachable");
            throw new InvalidOperationException();
#endif
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperation_NotNestedType()
        {
#if SYSTEM_REFLECTION_METADATA
            throw new InvalidOperationException(SR.InvalidOperation_NotNestedType);
#else
            Debug.Fail("Expected to be unreachable");
            throw new InvalidOperationException();
#endif
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperation_NoElement()
        {
#if SYSTEM_REFLECTION_METADATA
            throw new InvalidOperationException(SR.InvalidOperation_NoElement);
#else
            Debug.Fail("Expected to be unreachable");
            throw new InvalidOperationException();
#endif
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperation_HasToBeArrayClass()
        {
#if SYSTEM_REFLECTION_METADATA
            throw new InvalidOperationException(SR.Argument_HasToBeArrayClass);
#else
            Debug.Fail("Expected to be unreachable");
            throw new InvalidOperationException();
#endif
        }

        [DoesNotReturn]
        internal static void ThrowInvalidOperation_NestedTypeNamespace()
        {
#if SYSTEM_REFLECTION_METADATA
            throw new InvalidOperationException(SR.InvalidOperation_NestedTypeNamespace);
#else
            Debug.Fail("Expected to be unreachable");
            throw new InvalidOperationException();
#endif
        }

        internal static bool IsMaxDepthExceeded(TypeNameParseOptions options, int depth)
#if SYSTEM_PRIVATE_CORELIB
            => false; // CoreLib does not enforce any limits
#else
            => depth > options.MaxNodes;
#endif

        internal static bool TryDive(TypeNameParseOptions options, ref int depth)
        {
            depth++;
            return !IsMaxDepthExceeded(options, depth);
        }

#if SYSTEM_REFLECTION_METADATA
        [DoesNotReturn]
        internal static void ThrowInvalidOperation_NotSimpleName(string fullName)
        {
            throw new InvalidOperationException(SR.Format(SR.Arg_NotSimpleTypeName, fullName));
        }
#endif
    }
}
