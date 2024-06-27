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
        internal const sbyte SZArray = -1;
        internal const sbyte Pointer = -2;
        internal const sbyte ByRef = -3;
        private const char EscapeCharacter = '\\';
#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> s_endOfFullTypeNameDelimitersSearchValues = SearchValues.Create("[]&*,+\\");
#endif

        internal static string GetGenericTypeFullName(ReadOnlySpan<char> fullTypeName, ReadOnlySpan<TypeName> genericArgs)
        {
            Debug.Assert(genericArgs.Length > 0);

            ValueStringBuilder result = new(stackalloc char[128]);
            result.Append(fullTypeName);

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

            static bool NeedsEscaping(char c) => c is '[' or ']' or '&' or '*' or ',' or '+' or EscapeCharacter;
        }

        internal static ReadOnlySpan<char> GetName(ReadOnlySpan<char> fullName)
        {
            int offset = fullName.LastIndexOfAny('.', '+');

            if (offset > 0 && fullName[offset - 1] == EscapeCharacter) // this should be very rare (IL Emit & pure IL)
            {
                offset = GetUnescapedOffset(fullName, startIndex: offset);
            }

            return offset < 0 ? fullName : fullName.Slice(offset + 1);

            static int GetUnescapedOffset(ReadOnlySpan<char> fullName, int startIndex)
            {
                int offset = startIndex;
                for (; offset >= 0; offset--)
                {
                    if (fullName[offset] is '.' or '+')
                    {
                        if (offset == 0 || fullName[offset - 1] != EscapeCharacter)
                        {
                            break;
                        }
                        offset--; // skip the escaping character
                    }
                }
                return offset;
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

        internal static string GetRankOrModifierStringRepresentation(int rankOrModifier, ref ValueStringBuilder builder)
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

                builder.Append('[');
                builder.Append(',', rankOrModifier - 1);
                builder.Append(']');
            }

            return builder.ToString();
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

        internal static bool TryGetTypeNameInfo(ref ReadOnlySpan<char> input, ref List<int>? nestedNameLengths, out int totalLength)
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
    }
}
