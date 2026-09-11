// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor
{
    /// <summary>Provides the ability for the user to define custom behavior when writing CBOR data.</summary>
    public sealed class CborWriterOptions
    {
        private CborConformanceMode _conformanceMode = CborConformanceMode.Strict;
        private int _maxDepth = -1;
        private int _initialCapacity = -1;

        /// <summary>
        ///   Initializes a new instance of the <see cref="CborWriterOptions"/> class.
        /// </summary>
        public CborWriterOptions()
        {
        }

        /// <summary>Gets or sets the conformance mode used when writing CBOR data.</summary>
        /// <value>The conformance mode. The default is <see cref="CborConformanceMode.Strict" />.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value" /> is not a defined <see cref="CborConformanceMode" /> value.
        /// </exception>
        public CborConformanceMode ConformanceMode
        {
            get => _conformanceMode;
            set
            {
                CborConformanceModeHelpers.Validate(value);
                _conformanceMode = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value that indicates whether the writer automatically converts
        ///   indefinite-length encodings into definite-length equivalents.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the writer should automatically convert indefinite-length
        ///   encodings; otherwise, <see langword="false" />.
        ///   The default is <see langword="false" />.
        /// </value>
        public bool ConvertIndefiniteLengthEncodings { get; set; }

        /// <summary>
        ///   Gets or sets a value that indicates whether multiple root-level CBOR data items are permitted.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the writer allows multiple root-level values;
        ///   otherwise, <see langword="false" />.
        ///   The default is <see langword="false" />.
        /// </value>
        public bool AllowMultipleRootLevelValues { get; set; }

        /// <summary>
        ///   Gets or sets the maximum depth allowed when writing CBOR data, with the default (-1)
        ///   indicating a runtime-chosen maximum depth.
        /// </summary>
        /// <remarks>
        ///   The MaxDepth value is primarily to limit the amount of work the writer performs in
        ///   validating data in methods such as <see cref="CborWriter.WriteEncodedValue"/>.
        /// </remarks>
        /// <value>
        ///   The maximum depth allowed when writing CBOR data,
        ///   or -1 to indicate a runtime-chosen maximum depth.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value" /> is less than -1.
        /// </exception>
        public int MaxDepth
        {
            get => _maxDepth;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
                _maxDepth = value;
            }
        }

        /// <summary>
        ///  Gets or sets the initial capacity, in bytes, of the buffer used when writing CBOR data.
        /// </summary>
        /// <value>
        ///   The initial capacity, in bytes, of the buffer used when writing CBOR data,
        ///   or -1 to indicate a runtime-chosen initial capacity.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value" /> is less than -1.
        /// </exception>
        public int InitialCapacity
        {
            get => _initialCapacity;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
                _initialCapacity = value;
            }
        }
    }
}
