// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.ReturnStatus
{
    /// <summary>
    /// Completion packet "RETURNSTATUS" token
    /// </summary>
    public class TDSReturnStatusToken : TDSPacketToken
    {
        /// <summary>
        /// Value of the status
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSReturnStatusToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSReturnStatusToken(int value)
        {
            // Apply properties
            Value = value;
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Read return value
            Value = TDSUtilities.ReadInt(source);

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.ReturnStatus);

            // Write value
            TDSUtilities.WriteInt(destination, Value);
        }
    }
}
