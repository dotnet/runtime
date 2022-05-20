// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.Globalization;

namespace System.Text.Json
{
    internal sealed class JsonSimpleNamingPolicy : JsonNamingPolicy
    {
        private readonly bool _lowercase;
        private readonly char _boundary;

        internal JsonSimpleNamingPolicy(bool lowercase, char boundary) =>
            (_lowercase, _boundary) = (lowercase, boundary);

        public override string ConvertName(string name)
        {
            var bufferLength = name.Length * 2;
            var buffer = bufferLength > 512
                ? ArrayPool<char>.Shared.Rent(bufferLength)
                : null;

            var resultLength = 0;
            Span<char> result = buffer is null
                ? stackalloc char[512]
                : buffer;

            void WriteWord(ref Span<char> result, ReadOnlySpan<char> word)
            {
                if (word.IsEmpty)
                    return;

                var required = result.IsEmpty
                    ? word.Length
                    : word.Length + 1;

                if (required >= result.Length)
                {
                    var bufferLength = result.Length * 2;
                    var bufferNew = ArrayPool<char>.Shared.Rent(bufferLength);

                    result.CopyTo(bufferNew);

                    if (buffer is not null)
                        ArrayPool<char>.Shared.Return(buffer);

                    buffer = bufferNew;
                }

                if (resultLength != 0)
                {
                    result[resultLength] = _boundary;
                    resultLength += 1;
                }

                var destination = result.Slice(resultLength);
                if (_lowercase)
                {
                    word.ToLowerInvariant(destination);
                }
                else
                {
                    word.ToUpperInvariant(destination);
                }

                resultLength += word.Length;
            }

            int first = 0;
            var chars = name.AsSpan();
            var previousCategory = CharCategory.Boundary;
            for (int index = 0; index < chars.Length; index++)
            {
                var current = chars[index];
                var currentCategoryUnicode = char.GetUnicodeCategory(current);
                if (currentCategoryUnicode == UnicodeCategory.SpaceSeparator ||
                    currentCategoryUnicode >= UnicodeCategory.ConnectorPunctuation &&
                    currentCategoryUnicode <= UnicodeCategory.OtherPunctuation)
                {
                    WriteWord(ref result, chars.Slice(first, index - first));

                    previousCategory = CharCategory.Boundary;
                    first = index + 1;

                    continue;
                }

                if (index + 1 < chars.Length)
                {
                    var next = chars[index + 1];
                    var currentCategory = currentCategoryUnicode switch
                    {
                        UnicodeCategory.LowercaseLetter => CharCategory.Lowercase,
                        UnicodeCategory.UppercaseLetter => CharCategory.Uppercase,
                        _ => previousCategory
                    };

                    if (currentCategory == CharCategory.Lowercase && char.IsUpper(next) ||
                        next == '_')
                    {
                        WriteWord(ref result, chars.Slice(first, index + 1));

                        previousCategory = CharCategory.Boundary;
                        first = index + 1;

                        continue;
                    }

                    if (previousCategory == CharCategory.Uppercase &&
                        currentCategoryUnicode == UnicodeCategory.UppercaseLetter &&
                        char.IsLower(next))
                    {
                        WriteWord(ref result, chars.Slice(first, index - first));

                        previousCategory = CharCategory.Boundary;
                        first = index;

                        continue;
                    }

                    previousCategory = currentCategory;
                }
            }

            WriteWord(ref result, chars.Slice(first));

            name = result
                .Slice(0, resultLength)
                .ToString();

            if (buffer is not null)
                ArrayPool<char>.Shared.Return(buffer);

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
