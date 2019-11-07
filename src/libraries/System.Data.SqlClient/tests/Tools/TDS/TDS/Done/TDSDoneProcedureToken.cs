// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.Done
{
    /// <summary>
    /// Completion packet "DONEPROC" token
    /// </summary>
    public class TDSDoneProcedureToken : TDSPacketToken
    {
        /// <summary>
        /// Status of the completion
        /// </summary>
        public TDSDoneTokenStatusType Status { get; set; }

        /// <summary>
        /// Token for which completion is indicated
        /// </summary>
        public TDSDoneTokenCommandType Command { get; set; }

        /// <summary>
        /// Amount of rows returned
        /// </summary>
        public ulong RowCount { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSDoneProcedureToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSDoneProcedureToken(TDSDoneTokenStatusType status)
        {
            // Apply properties
            Status = status;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSDoneProcedureToken(TDSDoneTokenStatusType status, TDSDoneTokenCommandType command) :
            this(status)
        {
            // Apply properties that weren't applied by nested constructor call
            Command = command;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSDoneProcedureToken(TDSDoneTokenStatusType status, TDSDoneTokenCommandType command, ulong rowCount) :
            this(status, command)
        {
            // Apply properties that weren't applied by nested constructor call
            RowCount = rowCount;
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Read status
            Status = (TDSDoneTokenStatusType)TDSUtilities.ReadUShort(source);

            // Read command
            Command = (TDSDoneTokenCommandType)TDSUtilities.ReadUShort(source);

            // Read row count
            RowCount = TDSUtilities.ReadULong(source);

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.DoneProcedure);

            // Write status
            TDSUtilities.WriteUShort(destination, (ushort)Status);

            // Write command
            TDSUtilities.WriteUShort(destination, (ushort)Command);

            // Write row count
            TDSUtilities.WriteULong(destination, RowCount);
        }
    }
}
