// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    /// <summary>
    /// Contains information about the location of PEM data.
    /// </summary>
    public readonly struct PemFields
    {
        internal PemFields(Range label, Range base64data, Range location, int decodedDataLength)
        {
            Location = location;
            DecodedDataLength = decodedDataLength;
            Base64Data = base64data;
            Label = label;
        }

        /// <summary>
        /// Gets the location of the PEM-encoded text, including the surrounding encapsulation boundaries.
        /// </summary>
        public Range Location { get; }

        /// <summary>
        /// Gets the location of the label.
        /// </summary>
        public Range Label { get; }

        /// <summary>
        /// Gets the location of the base-64 data inside of the PEM.
        /// </summary>
        public Range Base64Data { get; }

        /// <summary>
        /// Gets the size of the decoded base-64 data, in bytes.
        /// </summary>
        public int DecodedDataLength { get; }
    }
}
