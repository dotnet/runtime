// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.SqlServer.TDS.ColMetadata;

namespace Microsoft.SqlServer.TDS.Row
{
    /// <summary>
    /// Token that corresponds to the row of data with null-byte compression
    /// </summary>
    public class TDSNBCRowToken : TDSRowTokenBase
    {
        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSNBCRowToken(TDSColMetadataToken metadata) :
            base(metadata)
        {
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // NOTE: We skip the identifier because it was read by the factory

            // Calculate the number of bytes necessary to contain all columns
            int byteCount = (int)Math.Ceiling((double)Metadata.Columns.Count / 8); /* bits in a byte */

            // Allocate a bit mask
            byte[] bitMask = new byte[byteCount];

            // Read the bit mask
            if (source.Read(bitMask, 0, bitMask.Length) != bitMask.Length)
            {
                // We don't suppot continuation of inflation at this point
                throw new Exception("Failed to inflate the bit mask of NBC row token");
            }

            // Process each column
            for (int index = 0; index < Metadata.Columns.Count; index++)
            {
                // Check if column is null. Null columns have corresponding bits set
                if (((bitMask[(int)(index / 8)] >> (index % 8)) & 0x01) != 0)
                {
                    // Data is null
                    Data.Add(null);
                }
                else
                {
                    // Inflate and add data to the list
                    Data.Add(InflateColumn(source, Metadata.Columns[index]));
                }
            }

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Serialize token identifier
            destination.WriteByte((byte)TDSTokenType.NBCRow);

            // Calculate the number of bytes necessary to contain all columns
            int byteCount = (int)Math.Ceiling((double)Metadata.Columns.Count / 8); /* bits in a byte */

            // Allocate a bit mask
            byte[] bitMask = new byte[byteCount];

            // Fist pass - figure out which column values are null
            for (int index = 0; index < Data.Count; index++)
            {
                // Check if data is null
                if (Data[index] == null)
                {
                    // Enable the corresponding bit
                    bitMask[(int)(index / 8)] = (byte)(bitMask[(int)(index / 8)] | (0x01 << (index % 8)));
                }
            }

            // Serialize the bit mask
            destination.Write(bitMask, 0, bitMask.Length);

            // Process each column
            for (int index = 0; index < Data.Count; index++)
            {
                // Check if data isn't null
                if (Data[index] != null)
                {
                    // Deflate each column
                    DeflateColumn(destination, Metadata.Columns[index], Data[index]);
                }
            }
        }
    }
}
