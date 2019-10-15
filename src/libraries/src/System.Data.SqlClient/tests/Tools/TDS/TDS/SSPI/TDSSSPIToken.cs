// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.SSPI
{
    /// <summary>
    /// Token that carries SSPI payload during login sequence
    /// </summary>
    public class TDSSSPIToken : TDSPacketToken
    {
        /// <summary>
        /// SSPI payload
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSSPIToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSSSPIToken(byte[] payload)
        {
            Payload = payload;
        }

        /// <summary>
        /// Inflate the token
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Read the payload length
            ushort length = TDSUtilities.ReadUShort(source);

            // Allocate buffer
            Payload = new byte[length];

            // Read payload
            source.Read(Payload, 0, Payload.Length);

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.SSPI);

            // Write length
            TDSUtilities.WriteUShort(destination, (ushort)((Payload != null) ? Payload.Length : 0));

            // Check if data is available
            if (Payload != null)
            {
                // Write the data
                destination.Write(Payload, 0, Payload.Length);
            }
        }
    }
}
