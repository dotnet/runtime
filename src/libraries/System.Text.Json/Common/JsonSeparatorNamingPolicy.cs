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

        public override string ConvertName(string name)
        {
            // Rented buffer 20% longer that the input.
            int bufferLength = (12 * name.Length) / 10;
            char[]? buffer = bufferLength > JsonConstants.StackallocCharThreshold
                ? ArrayPool<char>.Shared.Rent(bufferLength)
                : null;

            int resultLength = 0;
            Span<char> result = buffer is null
                ? stackalloc char[JsonConstants.StackallocCharThreshold]
                : buffer;

            void ExpandBuffer(ref Span<char> result)
            {
                var bufferNew = ArrayPool<char>.Shared.Rent(bufferLength *= 2);

                result.CopyTo(bufferNew);

                if (buffer is not null)
                {
                    ArrayPool<char>.Shared.Return(buffer, clearArray: true);
                }

                buffer = bufferNew;
                result = buffer;
            }

            void WriteWord(ReadOnlySpan<char> word, ref Span<char> result)
            {
                if (word.IsEmpty)
                {
                    return;
                }

                Span<char> destination = result.Slice(resultLength != 0
                    ? resultLength + 1
                    : resultLength);

                int written;
                while (true)
                {
                    written = _lowercase
                        ? word.ToLowerInvariant(destination)
                        : word.ToUpperInvariant(destination);

                    if (written > 0)
                    {
                        break;
                    }

                    ExpandBuffer(ref result);
                }

                if (resultLength != 0)
                {
                    result[resultLength] = _separator;
                    resultLength += 1;
                }

                resultLength += written;
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

            name = result.Slice(0, resultLength).ToString();

            if (buffer is not null)
            {
                ArrayPool<char>.Shared.Return(buffer, clearArray: true);
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
