// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Asn1
{
    /// <summary>
    ///   Specifies options that modify the behavior of an <see cref="AsnReader"/>.
    /// </summary>
    public struct AsnReaderOptions
    {
        private const int DefaultTwoDigitMax = 2049;

        private ushort _twoDigitYearMax;

        /// <summary>
        ///   Gets or sets the largest year to represent with a UtcTime value.
        /// </summary>
        /// <value>The largest year to represent with a UtcTime value. The default is 2049.</value>
        public int UtcTimeTwoDigitYearMax
        {
            get
            {
                if (_twoDigitYearMax == 0)
                {
                    return DefaultTwoDigitMax;
                }

                return _twoDigitYearMax;
            }
            set
            {
                if (value < 1 || value > 9999)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _twoDigitYearMax = (ushort)value;
            }
        }

        /// <summary>
        ///   Gets or sets a value that indicates whether the reader should bypass sort ordering
        ///   on a Set or Set-Of value.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the reader should not validate that a Set or Set-Of value
        ///   is sorted correctly for the current encoding rules; otherwise <see langword="false"/>.
        ///   The default is <see langword="false"/>.
        /// </value>
        public bool SkipSetSortOrderVerification { get; set; }
    }
}
