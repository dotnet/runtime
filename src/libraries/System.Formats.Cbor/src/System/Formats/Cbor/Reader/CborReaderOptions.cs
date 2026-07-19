// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor
{
    /// <summary>Provides the ability for the user to define custom behavior when reading CBOR data.</summary>
    public sealed class CborReaderOptions
    {
        private CborConformanceMode _conformanceMode = CborConformanceMode.Strict;
        private int _maxDepth = -1;

        /// <summary>
        ///   Initializes a new instance of the <see cref="CborReaderOptions"/> class.
        /// </summary>
        public CborReaderOptions()
        {
        }

        /// <summary>Gets or sets the conformance mode used when reading CBOR data.</summary>
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
        ///   Gets or sets a value that indicates whether multiple root-level values are supported by the reader.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if multiple root-level values are supported; otherwise, <see langword="false" />.
        ///   The default is <see langword="false" />.
        /// </value>
        public bool AllowMultipleRootLevelValues { get; set; }

        /// <summary>
        ///   Gets or sets the maximum depth allowed when reading CBOR data,
        ///   with the default (-1) indicating the maximum depth should be chosen by the runtime.</summary>
        /// <value>The maximum depth allowed when reading CBOR data.</value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value" /> is less than -1.</exception>
        public int MaxDepth
        {
            get => _maxDepth;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
                _maxDepth = value;
            }
        }
    }
}
