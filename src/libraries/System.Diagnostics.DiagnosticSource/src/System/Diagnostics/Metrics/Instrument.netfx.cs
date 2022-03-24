// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Instrument{T} is the base class from which all non-observable instruments will inherit from.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public abstract partial class Instrument<T> : Instrument where T : struct
    {
        [ThreadStatic]
        private static KeyValuePair<string, object?>[] ts_tags;

        private const int MaxTagsCount = 8;

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 1));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 2));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 3));
            ts_tags = tags;
        }

         /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tagList">A <see cref="T:System.Diagnostics.TagList" /> of tags associated with the measurement.</param>
       protected void RecordMeasurement(T measurement, in TagList tagList)
        {
            KeyValuePair<string, object?>[]? tags = tagList.Tags;
            if (tags is not null)
            {
                RecordMeasurement(measurement, tags.AsSpan().Slice(0, tagList.Count));
                return;
            }

            tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            Debug.Assert(tagList.Count <= MaxTagsCount);
            switch (tagList.Count)
            {
                case 8: tags[7] = tagList.Tag8; goto case 7;
                case 7: tags[6] = tagList.Tag7; goto case 6;
                case 6: tags[5] = tagList.Tag6; goto case 5;
                case 5: tags[4] = tagList.Tag5; goto case 4;
                case 4: tags[3] = tagList.Tag4; goto case 3;
                case 3: tags[2] = tagList.Tag3; goto case 2;
                case 2: tags[1] = tagList.Tag2; goto case 1;
                case 1: tags[0] = tagList.Tag1; break;
                case 0: return; // no need to report anything
                default:
                    Debug.Assert(false);
                    return;
            }

            ts_tags = null;

            RecordMeasurement(measurement, tags.AsSpan().Slice(0, tagList.Count));

            ts_tags = tags;
        }
    }
}
