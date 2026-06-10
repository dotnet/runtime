// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor
{
    /// <summary>Provides the options to be used with a <see cref="CborWriter" /> instance.</summary>
    public sealed class CborWriterOptions
    {
        private CborConformanceMode _conformanceMode = CborConformanceMode.Strict;
        private int _maxDepth;
        private int _initialCapacity;

        /// <summary>Initializes a new instance of the <see cref="CborWriterOptions" /> class.</summary>
        public CborWriterOptions()
        {
        }

        /// <summary>Gets or sets a value that indicates whether the writer allows multiple root-level CBOR data items.</summary>
        /// <value><see langword="true" /> if the writer allows multiple root-level CBOR data items; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        public bool AllowMultipleRootLevelValues { get; set; }

        /// <summary>Gets or sets the conformance mode used by the writer.</summary>
        /// <value>One of the enumeration values that represents the conformance mode used by the writer. The default is <see cref="CborConformanceMode.Strict" />.</value>
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

        /// <summary>Gets or sets a value that indicates whether the writer automatically converts indefinite-length encodings into definite-length equivalents.</summary>
        /// <value><see langword="true" /> if the writer automatically converts indefinite-length encodings into definite-length equivalents; otherwise, <see langword="false" />. The default is <see langword="false" />.</value>
        public bool ConvertIndefiniteLengthEncodings { get; set; }

        /// <summary>Gets or sets the initial capacity of the underlying buffer used by the writer.</summary>
        /// <value>The initial capacity of the underlying buffer used by the writer. A value of <c>0</c> indicates that no buffer should be preallocated. The default is <c>0</c>.</value>
        /// <exception cref="ArgumentOutOfRangeException">The specified value is negative.</exception>
        public int InitialCapacity
        {
            get => _initialCapacity;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _initialCapacity = value;
            }
        }

        /// <summary>Gets or sets the maximum depth allowed when writing nested CBOR data items.</summary>
        /// <value>The maximum depth allowed when writing nested CBOR data items. A value of <c>0</c> indicates that the writer should use its default maximum depth. The default is <c>0</c>.</value>
        /// <exception cref="ArgumentOutOfRangeException">The specified value is negative.</exception>
        /// <remarks>This setting is intended to guard against unbounded nesting caused by runaway encoders or serialization of cyclic object graphs. It is not a defense against writing already-hydrated hostile object graphs; you should not infer that any other use is supported.</remarks>
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
