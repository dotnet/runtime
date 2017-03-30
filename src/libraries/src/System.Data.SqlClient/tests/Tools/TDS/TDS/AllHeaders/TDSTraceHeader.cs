// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.AllHeaders
{
    /// <summary>
    /// Represents correlate activity ID trace header of ALL_HEADERS token
    /// </summary>
    public class TDSTraceHeader : TDSPacketToken
    {
        /// <summary>
        /// Identifier of the client activity
        /// </summary>
        public Guid ActivityID { get; set; }

        /// <summary>
        /// Sequential number of the operation
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSTraceHeader()
        {
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        public override bool Inflate(Stream source)
        {
            byte[] guidBytes = new byte[16];

            // Read exactly the size of GUID bytes
            if (source.Read(guidBytes, 0, guidBytes.Length) != guidBytes.Length)
            {
                throw new Exception("Failed to read trace GUID");
            }

            // Inflate trace identifier
            ActivityID = new Guid(guidBytes);

            // Read activity identifier
            SequenceNumber = TDSUtilities.ReadUInt(source);

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Allocate temporary cache
            MemoryStream cache = new MemoryStream();

            // Write header type
            TDSUtilities.WriteUShort(cache, (ushort)TDSHeaderType.Trace);

            byte[] guidBytes = new byte[16];

            // Check if activity ID is available
            if (ActivityID != null)
            {
                guidBytes = ActivityID.ToByteArray();
            }

            // Write trace identifier
            cache.Write(guidBytes, 0, guidBytes.Length);

            // Write activity identifier
            TDSUtilities.WriteUInt(cache, SequenceNumber);

            // Write the header length including self into the destination
            TDSUtilities.WriteUInt(destination, (uint)(cache.Length + 4));

            // Write cached header data
            cache.WriteTo(destination);
        }
    }
}
