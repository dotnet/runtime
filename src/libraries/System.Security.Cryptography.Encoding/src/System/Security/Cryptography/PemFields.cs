// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// The location of the PEM, including the surrounding ecapsulation boundaries.
        /// </summary>
        public Range Location { get; }

        /// <summary>
        /// The location of the label.
        /// </summary>
        public Range Label { get; }

        /// <summary>
        /// The location of the base64 data inside of the PEM.
        /// </summary>
        public Range Base64Data { get; }

        /// <summary>
        /// The size of the decoded base 64, in bytes.
        /// </summary>
        public int DecodedDataLength { get; }
    }
}
