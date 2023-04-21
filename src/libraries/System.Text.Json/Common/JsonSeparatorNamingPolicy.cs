// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;

namespace System.Text.Json
{
    internal abstract class JsonSeparatorNamingPolicy : JsonNamingPolicy
    {
        private readonly bool _lowercase;
        private readonly char _separator;

        internal JsonSeparatorNamingPolicy(bool lowercase, char separator) =>
            (_lowercase, _separator) = (lowercase, separator);

        public sealed override string ConvertName(string name)
        {
            if (name is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(name));
            }

            // Rented buffer 20% longer that the input.
            int rentedBufferLength = (12 * name.Length) / 10;
            char[]? rentedBuffer = rentedBufferLength > JsonConstants.StackallocCharThreshold
                ? ArrayPool<char>.Shared.Rent(rentedBufferLength)
                : null;

            int resultUsedLength = 0;
            Span<char> result = rentedBuffer is null
                ? stackalloc char[JsonConstants.StackallocCharThreshold]
                : rentedBuffer;

            void ExpandBuffer(ref Span<char> result)
            {
                char[] newBuffer = ArrayPool<char>.Shared.Rent(result.Length * 2);

                result.CopyTo(newBuffer);

                if (rentedBuffer is not null)
                {
                    result.Slice(0, resultUsedLength).Clear();
                    ArrayPool<char>.Shared.Return(rentedBuffer);
                }

                rentedBuffer = newBuffer;
                result = rentedBuffer;
            }

            void WriteWord(ReadOnlySpan<char> word, ref Span<char> result)
            {
                if (word.IsEmpty)
                {
                    return;
                }

                int written;
                while (true)
                {
                    var destinationOffset = resultUsedLength != 0
                        ? resultUsedLength + 1
                        : resultUsedLength;

                    if (destinationOffset < result.Length)
                    {
                        Span<char> destination = result.Slice(destinationOffset);

                        written = _lowercase
                            ? word.ToLowerInvariant(destination)
                            : word.ToUpperInvariant(destination);

                        if (written > 0)
                        {
                            break;
                        }
                    }

                    ExpandBuffer(ref result);
                }

                if (resultUsedLength != 0)
                {
                    result[resultUsedLength] = _separator;
                    resultUsedLength += 1;
                }

                resultUsedLength += written;
            }

            int first = 0;
            ReadOnlySpan<char> chars = name.AsSpan();
            CharCategory previousCategory = CharCategory.Boundary;

            for (int index = 0; index < chars.Length; index++)
            {
                char current = chars[index];
                UnicodeCategory currentCategoryUnicode = char.GetUnicodeCategory(current);

                if (currentCategoryUnicode == UnicodeCategory.SpaceSeparator ||
                    currentCategoryUnicode >= UnicodeCategory.ConnectorPunctuation &&
                    currentCategoryUnicode <= UnicodeCategory.OtherPunctuation)
                {
                    WriteWord(chars.Slice(first, index - first), ref result);

                    previousCategory = CharCategory.Boundary;
                    first = index + 1;

                    continue;
                }

                if (index + 1 < chars.Length)
                {
                    char next = chars[index + 1];
                    CharCategory currentCategory = currentCategoryUnicode switch
                    {
                        UnicodeCategory.LowercaseLetter => CharCategory.Lowercase,
                        UnicodeCategory.UppercaseLetter => CharCategory.Uppercase,
                        _ => previousCategory
                    };

                    if (currentCategory == CharCategory.Lowercase && char.IsUpper(next) ||
                        next == '_')
                    {
                        WriteWord(chars.Slice(first, index - first + 1), ref result);

                        previousCategory = CharCategory.Boundary;
                        first = index + 1;

                        continue;
                    }

                    if (previousCategory == CharCategory.Uppercase &&
                        currentCategoryUnicode == UnicodeCategory.UppercaseLetter &&
                        char.IsLower(next))
                    {
                        WriteWord(chars.Slice(first, index - first), ref result);

                        previousCategory = CharCategory.Boundary;
                        first = index;

                        continue;
                    }

                    previousCategory = currentCategory;
                }
            }

            WriteWord(chars.Slice(first), ref result);

            name = result.Slice(0, resultUsedLength).ToString();

            if (rentedBuffer is not null)
            {
                result.Slice(0, resultUsedLength).Clear();
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }

            return name;
        }

        private enum CharCategory
        {
            Boundary,
            Lowercase,
            Uppercase,
        }
    }
}
