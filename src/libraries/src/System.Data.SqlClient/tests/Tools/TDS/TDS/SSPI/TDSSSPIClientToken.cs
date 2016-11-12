// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.SSPI
{
    /// <summary>
    /// Token that carries client's SSPI payload during login sequence 
    /// </summary>
    public class TDSSSPIClientToken : TDSPacketToken
    {
        /// <summary>
        /// Data length
        /// </summary>
        private int _length;

        /// <summary>
        /// Current offset at which inflation occurs
        /// </summary>
        private int _inflationOffset;

        /// <summary>
        /// SSPI payload
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSSPIClientToken(Stream source, int length)
        {
            // Allocate buffer
            Payload = new byte[length];

            // Save the length to enable inflation
            _length = length;

            // Inflate
            if (!Inflate(source))
            {
                // There was not enough data to inflate the token
                throw new Exception(string.Format("Failed to inflate client SSPI token. Inflated {0} of {1} byte(s)", _inflationOffset, _length));
            }
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSSSPIClientToken(byte[] payload)
        {
            // Save payload
            Payload = payload;

            // Not really used, but usefull anyway
            _length = Payload.Length;

            // Mark that the "inflation" is complete
            _inflationOffset = _length;
        }

        /// <summary>
        /// Inflate the token
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Check if inflation is complete
            if (_inflationOffset >= _length)
            {
                // We're done
                return true;
            }

            // Read payload
            int readSize = source.Read(Payload, _inflationOffset, Payload.Length - _inflationOffset);

            // Update inflation offset with the new read size
            _inflationOffset += readSize;

            // Check if we're done
            return (_inflationOffset >= _length);
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write the data
            destination.Write(Payload, 0, Payload.Length);
        }
    }
}
