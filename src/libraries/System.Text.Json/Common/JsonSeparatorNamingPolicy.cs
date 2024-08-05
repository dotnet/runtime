// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal abstract class JsonSeparatorNamingPolicy : JsonNamingPolicy
    {
        private readonly bool _lowercase;
        private readonly char _separator;

        internal JsonSeparatorNamingPolicy(bool lowercase, char separator)
        {
            Debug.Assert(char.IsPunctuation(separator));

            _lowercase = lowercase;
            _separator = separator;
        }

        public sealed override string ConvertName(string name)
        {
            if (name is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(name));
            }

            return ConvertNameCore(_separator, _lowercase, name.AsSpan());
        }

        private static string ConvertNameCore(char separator, bool lowercase, ReadOnlySpan<char> chars)
        {
            char[]? rentedBuffer = null;

            // While we can't predict the expansion factor of the resultant string,
            // start with a buffer that is at least 20% larger than the input.
            int initialBufferLength = (int)(1.2 * chars.Length);
            Span<char> destination = initialBufferLength <= JsonConstants.StackallocCharThreshold
                ? stackalloc char[JsonConstants.StackallocCharThreshold]
                : (rentedBuffer = ArrayPool<char>.Shared.Rent(initialBufferLength));

            SeparatorState state = SeparatorState.NotStarted;
            int charsWritten = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                // NB this implementation does not handle surrogate pair letters
                // cf. https://github.com/dotnet/runtime/issues/90352

                char current = chars[i];
                UnicodeCategory category = char.GetUnicodeCategory(current);

                switch (category)
                {
                    case UnicodeCategory.UppercaseLetter:

                        switch (state)
                        {
                            case SeparatorState.NotStarted:
                                break;

                            case SeparatorState.LowercaseLetterOrDigit:
                            case SeparatorState.SpaceSeparator:
                                // An uppercase letter following a sequence of lowercase letters or spaces
                                // denotes the start of a new grouping: emit a separator character.
                                WriteChar(separator, ref destination);
                                break;

                            case SeparatorState.UppercaseLetter:
                                // We are reading through a sequence of two or more uppercase letters.
                                // Uppercase letters are grouped together with the exception of the
                                // final letter, assuming it is followed by lowercase letters.
                                // For example, the value 'XMLReader' should render as 'xml_reader',
                                // however 'SHA512Hash' should render as 'sha512-hash'.
                                if (i + 1 < chars.Length && char.IsLower(chars[i + 1]))
                                {
                                    WriteChar(separator, ref destination);
                                }
                                break;

                            default:
                                Debug.Fail($"Unexpected state {state}");
                                break;
                        }

                        if (lowercase)
                        {
                            current = char.ToLowerInvariant(current);
                        }

                        WriteChar(current, ref destination);
                        state = SeparatorState.UppercaseLetter;
                        break;

                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:

                        if (state is SeparatorState.SpaceSeparator)
                        {
                            // Normalize preceding spaces to one separator.
                            WriteChar(separator, ref destination);
                        }

                        if (!lowercase && category is UnicodeCategory.LowercaseLetter)
                        {
                            current = char.ToUpperInvariant(current);
                        }

                        WriteChar(current, ref destination);
                        state = SeparatorState.LowercaseLetterOrDigit;
                        break;

                    case UnicodeCategory.SpaceSeparator:
                        // Space characters are trimmed from the start and end of the input string
                        // but are normalized to separator characters if between letters.
                        if (state != SeparatorState.NotStarted)
                        {
                            state = SeparatorState.SpaceSeparator;
                        }
                        break;

                    default:
                        // Non-alphanumeric characters (including the separator character and surrogates)
                        // are written as-is to the output and reset the separator state.
                        // E.g. 'ABC???def' maps to 'abc???def' in snake_case.

                        WriteChar(current, ref destination);
                        state = SeparatorState.NotStarted;
                        break;
                }
            }

            string result = destination.Slice(0, charsWritten).ToString();

            if (rentedBuffer is not null)
            {
                destination.Slice(0, charsWritten).Clear();
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }

            return result;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void WriteChar(char value, ref Span<char> destination)
            {
                if (charsWritten == destination.Length)
                {
                    ExpandBuffer(ref destination);
                }

                destination[charsWritten++] = value;
            }

            void ExpandBuffer(ref Span<char> destination)
            {
                int newSize = checked(destination.Length * 2);
                char[] newBuffer = ArrayPool<char>.Shared.Rent(newSize);
                destination.CopyTo(newBuffer);

                if (rentedBuffer is not null)
                {
                    destination.Slice(0, charsWritten).Clear();
                    ArrayPool<char>.Shared.Return(rentedBuffer);
                }

                rentedBuffer = newBuffer;
                destination = rentedBuffer;
            }
        }

        private enum SeparatorState
        {
            NotStarted,
            UppercaseLetter,
            LowercaseLetterOrDigit,
            SpaceSeparator,
        }
    }
}
