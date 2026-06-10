// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor
{
    /// <summary>Provides the options to be used with a <see cref="CborReader" /> instance.</summary>
    public sealed class CborReaderOptions
    {
        private CborConformanceMode _conformanceMode = CborConformanceMode.Strict;
        private int _maxDepth;

        /// <summary>Initializes a new instance of the <see cref="CborReaderOptions" /> class.</summary>
        public CborReaderOptions()
        {
        }

        /// <summary>Gets or sets a value that indicates whether the reader allows multiple root-level CBOR data items.</summary>
        /// <value><see langword="true" /> if the reader allows multiple root-level CBOR data items; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        public bool AllowMultipleRootLevelValues { get; set; }

        /// <summary>Gets or sets the conformance mode used by the reader.</summary>
        /// <value>One of the enumeration values that represents the conformance mode used by the reader. The default is <see cref="CborConformanceMode.Strict" />.</value>
        /// <exception cref="ArgumentOutOfRangeException">The specified value is not a defined <see cref="CborConformanceMode" />.</exception>
        public CborConformanceMode ConformanceMode
        {
            get => _conformanceMode;
            set
            {
                CborConformanceModeHelpers.Validate(value);
                _conformanceMode = value;
            }
        }

        /// <summary>Gets or sets the maximum depth allowed when reading nested CBOR data items.</summary>
        /// <value>The maximum depth allowed when reading nested CBOR data items. A value of <c>0</c> indicates that the reader should use its default maximum depth. The default is <c>0</c>.</value>
        /// <exception cref="ArgumentOutOfRangeException">The specified value is negative.</exception>
        /// <remarks>Limiting the maximum depth guards against stack overflow and excessive resource consumption when reading deeply nested CBOR data from untrusted sources.</remarks>
        public int MaxDepth
        {
            get => _maxDepth;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _maxDepth = value;
            }
        }
    }
}
