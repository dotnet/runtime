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
        private readonly Rune _separator;

        internal JsonSeparatorNamingPolicy(bool lowercase, char separator)
        {
            Debug.Assert(char.IsPunctuation(separator));

            _lowercase = lowercase;
            _separator = new Rune(separator);
        }

        public sealed override string ConvertName(string name)
        {
            if (name is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(name));
            }

            return ConvertNameCore(_separator, _lowercase, name);
        }

        private static string ConvertNameCore(Rune separator, bool lowercase, string name)
        {
            Debug.Assert(name != null);

            char[]? rentedBuffer = null;

            // While we can't predict the expansion factor of the resultant string,
            // start with a buffer that is at least 20% larger than the input.
            int initialBufferLength = (int)(1.2 * name.Length);
            Span<char> destination = initialBufferLength <= JsonConstants.StackallocCharThreshold
                ? stackalloc char[JsonConstants.StackallocCharThreshold]
                : (rentedBuffer = ArrayPool<char>.Shared.Rent(initialBufferLength));

            SeparatorState state = SeparatorState.NotStarted;
            int charsWritten = 0;

            for (int i = 0; i < name.Length;)
            {
                Rune current = Rune.GetRuneAt(name, i);
                int charLength = current.Utf16SequenceLength;

                switch (Rune.GetUnicodeCategory(current))
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
                                Write(separator, ref destination);
                                break;

                            case SeparatorState.UppercaseLetter:
                                // We are reading through a sequence of two or more uppercase letters.
                                // Uppercase letters are grouped together with the exception of the
                                // final letter, assuming it is followed by lowercase letters.
                                // For example, the value 'XMLReader' should render as 'xml_reader',
                                // however 'SHA512Hash' should render as 'sha512-hash'.
                                if (i + charLength < name.Length)
                                {
                                    Rune next = Rune.GetRuneAt(name, i + charLength);
                                    if (Rune.GetUnicodeCategory(next) is UnicodeCategory.LowercaseLetter)
                                    {
                                        Write(separator, ref destination);
                                    }
                                }

                                break;

                            default:
                                Debug.Fail($"Unexpected state {state}");
                                break;
                        }

                        if (lowercase)
                        {
                            current = Rune.ToLowerInvariant(current);
                        }

                        Write(current, ref destination);
                        state = SeparatorState.UppercaseLetter;
                        break;

                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:

                        if (state is SeparatorState.SpaceSeparator)
                        {
                            // Normalize preceding spaces to one separator.
                            Write(separator, ref destination);
                        }

                        if (!lowercase)
                        {
                            current = Rune.ToUpperInvariant(current);
                        }

                        Write(current, ref destination);
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
                        // Non-alphanumeric characters (including the separator character itself)
                        // are written as-is to the output and reset the separator state.
                        // E.g. 'ABC???def' maps to 'abc???def' in snake_case.

                        Write(current, ref destination);
                        state = SeparatorState.NotStarted;
                        break;
                }

                i += charLength;
            }

            name = destination.Slice(0, charsWritten).ToString();

            if (rentedBuffer is not null)
            {
                destination.Slice(0, charsWritten).Clear();
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }

            return name;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Write(Rune rune, ref Span<char> destination)
            {
                if (charsWritten + 2 > destination.Length)
                {
                    ExpandBuffer(ref destination);
                }

                int written = rune.EncodeToUtf16(destination.Slice(charsWritten));
                Debug.Assert(written == rune.Utf16SequenceLength);
                charsWritten += written;
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

#if !NETCOREAPP
        // Provides a basic Rune polyfill that handles surrogate pairs
        // TODO remove once https://github.com/dotnet/runtime/issues/52947 is complete.
        private readonly struct Rune
        {
            private readonly char _first;
            private readonly char? _lowSurrogate;
            private readonly UnicodeCategory _category;

            public int Utf16SequenceLength => _lowSurrogate.HasValue ? 2 : 1;

            public static UnicodeCategory GetUnicodeCategory(Rune rune)
                => rune._category;

            public Rune(char ch) : this(ch, char.GetUnicodeCategory(ch))
            {
            }

            private Rune(char ch, UnicodeCategory category)
            {
                Debug.Assert(!char.IsSurrogate(ch));
                _first = ch;
                _category = category;
            }

            private Rune(char highSurrogate, char lowSurrogate, UnicodeCategory category)
            {
                Debug.Assert(char.IsSurrogatePair(highSurrogate, lowSurrogate));
                _first = highSurrogate;
                _lowSurrogate = lowSurrogate;
                _category = category;
            }

            public static Rune ToLowerInvariant(Rune value)
            {
                UnicodeCategory category = value._category;
                if (category is UnicodeCategory.UppercaseLetter)
                {
                    category = UnicodeCategory.LowercaseLetter;
                }

                if (value._lowSurrogate is not char lowSurrogate)
                {
                    return new Rune(char.ToLowerInvariant(value._first), category);
                }

                ReadOnlySpan<char> source = stackalloc char[] { value._first, lowSurrogate };
                Span<char> destination = stackalloc char[2];

                source.ToLowerInvariant(destination);
                return new Rune(destination[0], destination[1], category);
            }

            public static Rune ToUpperInvariant(Rune value)
            {
                UnicodeCategory category = value._category;
                if (category is UnicodeCategory.LowercaseLetter)
                {
                    category = UnicodeCategory.UppercaseLetter;
                }

                if (value._lowSurrogate is not char lowSurrogate)
                {
                    return new Rune(char.ToUpperInvariant(value._first), category);
                }

                ReadOnlySpan<char> source = stackalloc char[] { value._first, lowSurrogate };
                Span<char> destination = stackalloc char[2];

                source.ToUpperInvariant(destination);
                return new Rune(destination[0], destination[1], category);
            }

            public int EncodeToUtf16(Span<char> destination)
            {
                Debug.Assert(Utf16SequenceLength <= destination.Length);
                destination[0] = _first;

                if (_lowSurrogate is not char lowSurrogate)
                {
                    return 1;
                }

                destination[1] = lowSurrogate;
                return 2;
            }

            public static Rune GetRuneAt(string input, int index)
            {
                char first = input[index];
                UnicodeCategory category = char.GetUnicodeCategory(first);
                if (category is UnicodeCategory.Surrogate)
                {
                    char lowSurrogate = default;
                    if (index + 1 == input.Length ||
                        !char.IsSurrogatePair(first, lowSurrogate = input[index + 1]))
                    {
                        // CharUnicodeInfo.GetUnicodeCategory does
                        // not throw so we throw here instead.
                        ThrowArgumentException();

                        static void ThrowArgumentException() => throw new ArgumentException(nameof(input));
                    }

                    category = CharUnicodeInfo.GetUnicodeCategory(input, index);
                    return new Rune(first, lowSurrogate, category);
                }

                return new Rune(first, category);
            }
        }
#endif
        private enum SeparatorState
        {
            NotStarted,
            UppercaseLetter,
            LowercaseLetterOrDigit,
            SpaceSeparator,
        }
    }
}
