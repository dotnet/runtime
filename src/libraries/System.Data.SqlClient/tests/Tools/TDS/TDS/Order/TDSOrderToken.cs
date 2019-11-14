// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.Order
{
    /// <summary>
    /// Completion packet "ORDER" token
    /// </summary>
    public class TDSOrderToken : TDSPacketToken
    {
        /// <summary>
        /// Column number
        /// </summary>
        public ushort Number { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSOrderToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSOrderToken(ushort value)
        {
            // Apply properties
            Number = value;
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Read the stream length
            ushort length = TDSUtilities.ReadUShort(source);

            // Check if there's enough space for a short
            if (length < sizeof(ushort))
            {
                return false;
            }

            // Read return value
            Number = TDSUtilities.ReadUShort(source);

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.Order);

            // Write the length
            TDSUtilities.WriteUShort(destination, sizeof(ushort));

            // Write value
            TDSUtilities.WriteUShort(destination, Number);
        }
    }
}
