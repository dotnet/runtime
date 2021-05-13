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
        [ThreadStatic] private KeyValuePair<string, object?>[] ts_tags = new KeyValuePair<string, object?>[3];

        // 0 is not recursive, 1 is recursive.
        // We are using the thread static array ts_tags to report the measurements by calling the listener callback. We are using ts_recursive to protect against
        // the case if the listener callback decide to record extra measurement using the same thread.
        [ThreadStatic] private int ts_recursive;

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listeneing to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag)
        {
            int isRecursive = Interlocked.CompareExchange(ref ts_recursive, 1, 0);
            KeyValuePair<string, object?>[] tags = isRecursive == 0 ? ts_tags : new KeyValuePair<string, object?>[1];
            tags[0] = tag;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 1));
            if (isRecursive == 0) { ts_recursive = 0; }
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listeneing to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
        {
            int isRecursive = Interlocked.CompareExchange(ref ts_recursive, 1, 0);
            KeyValuePair<string, object?>[] tags = isRecursive == 0 ? ts_tags : new KeyValuePair<string, object?>[2];
            tags[0] = tag1;
            tags[1] = tag2;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 2));
            if (isRecursive == 0) { ts_recursive = 0; }
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listeneing to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
        {
            int isRecursive = Interlocked.CompareExchange(ref ts_recursive, 1, 0);
            KeyValuePair<string, object?>[] tags = isRecursive == 0 ? ts_tags : new KeyValuePair<string, object?>[3];
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 3));
            if (isRecursive == 0) { ts_recursive = 0; }
        }
    }
}