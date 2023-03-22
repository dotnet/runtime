// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    public class RangeConditionHeaderValue : ICloneable
    {
        // Exactly one of date and entityTag will be set.
        private readonly DateTimeOffset _date;
        private readonly EntityTagHeaderValue? _entityTag;

        public DateTimeOffset? Date => _entityTag is null ? _date : null;

        public EntityTagHeaderValue? EntityTag => _entityTag;

        public RangeConditionHeaderValue(DateTimeOffset date)
        {
            _date = date;
        }

        public RangeConditionHeaderValue(EntityTagHeaderValue entityTag)
        {
            ArgumentNullException.ThrowIfNull(entityTag);

            _entityTag = entityTag;
        }

        public RangeConditionHeaderValue(string entityTag)
            : this(new EntityTagHeaderValue(entityTag))
        {
        }

        private RangeConditionHeaderValue(RangeConditionHeaderValue source)
        {
            Debug.Assert(source != null);

            _entityTag = source._entityTag;
            _date = source._date;
        }

        public override string ToString() => _entityTag?.ToString() ?? _date.ToString("r");

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is RangeConditionHeaderValue other &&
            (_entityTag is null ? other._entityTag is null : _entityTag.Equals(other._entityTag)) &&
            _date == other._date;

        public override int GetHashCode() => _entityTag?.GetHashCode() ?? _date.GetHashCode();

        public static RangeConditionHeaderValue Parse(string input)
        {
            int index = 0;
            return (RangeConditionHeaderValue)GenericHeaderParser.RangeConditionParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RangeConditionHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (GenericHeaderParser.RangeConditionParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (RangeConditionHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetRangeConditionLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            // Make sure we have at least 2 characters
            if (string.IsNullOrEmpty(input) || (startIndex + 1 >= input.Length))
            {
                return 0;
            }

            int current = startIndex;

            // Caller must remove leading whitespace.
            DateTimeOffset date = DateTimeOffset.MinValue;
            EntityTagHeaderValue? entityTag = null;

            // Entity tags are quoted strings optionally preceded by "W/". By looking at the first two character we
            // can determine whether the string is en entity tag or a date.
            char firstChar = input[current];
            char secondChar = input[current + 1];

            if ((firstChar == '\"') || (((firstChar == 'w') || (firstChar == 'W')) && (secondChar == '/')))
            {
                // trailing whitespace is removed by GetEntityTagLength()
                int entityTagLength = EntityTagHeaderValue.GetEntityTagLength(input, current, out entityTag);

                if (entityTagLength == 0)
                {
                    return 0;
                }

                current += entityTagLength;

                // RangeConditionHeaderValue only allows 1 value. There must be no delimiter/other chars after an
                // entity tag.
                if (current != input.Length)
                {
                    return 0;
                }
            }
            else
            {
                if (!HttpDateParser.TryParse(input.AsSpan(current), out date))
                {
                    return 0;
                }

                // If we got a valid date, then the parser consumed the whole string (incl. trailing whitespace).
                current = input.Length;
            }

            if (entityTag == null)
            {
                parsedValue = new RangeConditionHeaderValue(date);
            }
            else
            {
                parsedValue = new RangeConditionHeaderValue(entityTag);
            }

            return current - startIndex;
        }

        object ICloneable.Clone()
        {
            return new RangeConditionHeaderValue(this);
        }
    }
}
