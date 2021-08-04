// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    // We define a separate structure for the different number of tags.
    // The reason is, the performance is critical for the Metrics APIs that accept tags parameters.
    // We are trying to reduce big tags structure initialization inside the APIs when using fewer tags.

    [StructLayout(LayoutKind.Sequential)]
    internal struct OneTagBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal OneTagBag(KeyValuePair<string, object?> tag)
        {
            Tag1 = tag;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TwoTagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal TwoTagsBag(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
        {
            Tag1 = tag1;
            Tag2 = tag2;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ThreeTagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
        internal ThreeTagsBag(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
        {
            Tag1 = tag1;
            Tag2 = tag2;
            Tag3 = tag3;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FourTagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
        internal KeyValuePair<string, object?> Tag4;
        internal FourTagsBag(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4)
        {
            Tag1 = tag1;
            Tag2 = tag2;
            Tag3 = tag3;
            Tag4 = tag4;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FiveTagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
        internal KeyValuePair<string, object?> Tag4;
        internal KeyValuePair<string, object?> Tag5;
        internal FiveTagsBag(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                             KeyValuePair<string, object?> tag5)
        {
            Tag1 = tag1;
            Tag2 = tag2;
            Tag3 = tag3;
            Tag4 = tag4;
            Tag5 = tag5;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SixTagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
        internal KeyValuePair<string, object?> Tag4;
        internal KeyValuePair<string, object?> Tag5;
        internal KeyValuePair<string, object?> Tag6;
        internal SixTagsBag(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                            KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6)
        {
            Tag1 = tag1;
            Tag2 = tag2;
            Tag3 = tag3;
            Tag4 = tag4;
            Tag5 = tag5;
            Tag6 = tag6;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SevenTagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
        internal KeyValuePair<string, object?> Tag4;
        internal KeyValuePair<string, object?> Tag5;
        internal KeyValuePair<string, object?> Tag6;
        internal KeyValuePair<string, object?> Tag7;
        internal SevenTagsBag(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                              KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6, KeyValuePair<string, object?> tag7)
        {
            Tag1 = tag1;
            Tag2 = tag2;
            Tag3 = tag3;
            Tag4 = tag4;
            Tag5 = tag5;
            Tag6 = tag6;
            Tag7 = tag7;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EightTagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
        internal KeyValuePair<string, object?> Tag4;
        internal KeyValuePair<string, object?> Tag5;
        internal KeyValuePair<string, object?> Tag6;
        internal KeyValuePair<string, object?> Tag7;
        internal KeyValuePair<string, object?> Tag8;
        internal EightTagsBag(KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                              KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6, KeyValuePair<string, object?> tag7, KeyValuePair<string, object?> tag8)
        {
            Tag1 = tag1;
            Tag2 = tag2;
            Tag3 = tag3;
            Tag4 = tag4;
            Tag5 = tag5;
            Tag6 = tag6;
            Tag7 = tag7;
            Tag8 = tag8;
        }
    }

    /// <summary>
    /// Instrument{T} is the base class from which all non-observable instruments will inherit from.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public abstract partial class Instrument<T> : Instrument where T : struct
    {
        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag)
        {
            OneTagBag tags = new OneTagBag(tag);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 1));
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
        {
            TwoTagsBag tags = new TwoTagsBag(tag1, tag2);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 2));
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
            ThreeTagsBag tags = new ThreeTagsBag(tag1, tag2, tag3);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 3));
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4)
        {
            FourTagsBag tags = new FourTagsBag(tag1, tag2, tag3, tag4);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 4));
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                        KeyValuePair<string, object?> tag5)
        {
            FiveTagsBag tags = new FiveTagsBag(tag1, tag2, tag3, tag4, tag5);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 5));
        }
        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        /// <param name="tag6">A sixth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                        KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6)
        {
            SixTagsBag tags = new SixTagsBag(tag1, tag2, tag3, tag4, tag5, tag6);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 6));
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        /// <param name="tag6">A sixth key-value pair tag associated with the measurement.</param>
        /// <param name="tag7">A seventh key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                        KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6, KeyValuePair<string, object?> tag7)
        {
            SevenTagsBag tags = new SevenTagsBag(tag1, tag2, tag3, tag4, tag5, tag6, tag7);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 7));
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        /// <param name="tag6">A sixth key-value pair tag associated with the measurement.</param>
        /// <param name="tag7">A seventh key-value pair tag associated with the measurement.</param>
        /// <param name="tag8">An eighth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                        KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6, KeyValuePair<string, object?> tag7, KeyValuePair<string, object?> tag8)
        {
            EightTagsBag tags = new EightTagsBag(tag1, tag2, tag3, tag4, tag5, tag6, tag7, tag8);

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 8));
        }
    }
}