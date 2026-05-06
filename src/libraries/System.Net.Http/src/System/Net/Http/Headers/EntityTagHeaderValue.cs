// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    public class EntityTagHeaderValue : ICloneable
    {
        public string Tag { get; private init; }

        public bool IsWeak { get; private init; }

        public static EntityTagHeaderValue Any { get; } = new EntityTagHeaderValue("*", isWeak: false, false);

        private EntityTagHeaderValue(string tag, bool isWeak, bool _)
        {
#if DEBUG
            // This constructor should only be used with already validated values.
            // "*" is a special case that can only be created via the static Any property.
            if (tag != "*")
            {
                new EntityTagHeaderValue(tag, isWeak);
            }
#endif

            Tag = tag;
            IsWeak = isWeak;
        }

        public EntityTagHeaderValue(string tag)
            : this(tag, false)
        {
        }

        public EntityTagHeaderValue(string tag, bool isWeak)
        {
            HeaderUtilities.CheckValidQuotedString(tag);

            Tag = tag;
            IsWeak = isWeak;
        }

        private EntityTagHeaderValue(EntityTagHeaderValue source)
        {
            Debug.Assert(source != null);

            Tag = source.Tag;
            IsWeak = source.IsWeak;
        }

        public override string ToString() =>
            IsWeak ? $"W/{Tag}" : Tag;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is EntityTagHeaderValue other &&
            IsWeak == other.IsWeak &&
            // Since the tag is a quoted-string we treat it case-sensitive.
            string.Equals(Tag, other.Tag, StringComparison.Ordinal);

        public override int GetHashCode() =>
            HashCode.Combine(Tag, IsWeak);

        public static EntityTagHeaderValue Parse(string input)
        {
            int index = 0;
            return (EntityTagHeaderValue)GenericHeaderParser.SingleValueEntityTagParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out EntityTagHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (GenericHeaderParser.SingleValueEntityTagParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (EntityTagHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetEntityTagLength(string? input, int startIndex, out EntityTagHeaderValue? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespace. If not, we'll return 0.
            bool isWeak = false;
            int current = startIndex;

            char firstChar = input[startIndex];
            if (firstChar == '*')
            {
                // We have '*' value, indicating "any" ETag.
                parsedValue = Any;
                current++;
            }
            else
            {
                // The RFC defines 'W/' as prefix, but we'll be flexible and also accept lower-case 'w'.
                if ((firstChar == 'W') || (firstChar == 'w'))
                {
                    current++;
                    // We need at least 3 more chars: the '/' character followed by two quotes.
                    if ((current + 2 >= input.Length) || (input[current] != '/'))
                    {
                        return 0;
                    }
                    isWeak = true;
                    current++; // we have a weak-entity tag.
                    current += HttpRuleParser.GetWhitespaceLength(input, current);
                }

                if (current == input.Length || HttpRuleParser.GetQuotedStringLength(input, current, out int tagLength) != HttpParseResult.Parsed)
                {
                    return 0;
                }

                // Most of the time we'll have strong ETags without leading/trailing whitespace.
                Debug.Assert(tagLength != input.Length || (startIndex == 0 && !isWeak));

                parsedValue = new EntityTagHeaderValue(input.Substring(current, tagLength), isWeak, false);

                current += tagLength;
            }
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            return current - startIndex;
        }

        object ICloneable.Clone() => ReferenceEquals(this, Any) ? Any : new EntityTagHeaderValue(this);
    }
}
