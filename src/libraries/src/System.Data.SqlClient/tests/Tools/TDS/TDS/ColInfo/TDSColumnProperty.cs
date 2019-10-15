// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.ColInfo
{
    /// <summary>
    /// Information about a single column
    /// </summary>
    public class TDSColumnProperty : IInflatable, IDeflatable
    {
        /// <summary>
        /// Number of bytes processed from the stream during inflation
        /// </summary>
        internal int InflationSize { get; private set; }

        /// <summary>
        /// Number of the column in the result
        /// </summary>
        public byte Number { get; set; }

        /// <summary>
        /// Number of the table
        /// </summary>
        public byte TableNumber { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public TDSColumnStatus Status { get; set; }

        /// <summary>
        /// Name of the column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public virtual bool Inflate(Stream source)
        {
            // Read column number
            Number = (byte)source.ReadByte();

            // Update offset with the read size
            InflationSize += sizeof(byte);

            // Read table number
            TableNumber = (byte)source.ReadByte();

            // Update offset with the read size
            InflationSize += sizeof(byte);

            // Read status
            Status = (TDSColumnStatus)source.ReadByte();

            // Update offset with the read size
            InflationSize += sizeof(byte);

            // Check if status indicates the table name
            if ((Status & TDSColumnStatus.DifferentName) != 0)
            {
                // Read the length of the table name
                byte tableNameLength = (byte)source.ReadByte();

                // Read table name
                Name = TDSUtilities.ReadString(source, (ushort)(tableNameLength * 2));
            }

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public virtual void Deflate(Stream destination)
        {
            // Write column number
            destination.WriteByte(Number);

            // Write table number
            destination.WriteByte(TableNumber);

            // Write status number
            destination.WriteByte((byte)Status);

            // Check if we have a name
            if (Name != null)
            {
                // Write length
                destination.WriteByte((byte)Name.Length);

                // Write data
                TDSUtilities.WriteString(destination, Name);
            }
        }
    }
}
