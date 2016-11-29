// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.SqlServer.TDS.ColMetadata
{
    /// <summary>
    /// Result set metadata description "COLMETADATA" token
    /// </summary>
    public class TDSColMetadataToken : TDSPacketToken
    {
        /// <summary>
        /// A collection of columns for which metadata is available
        /// </summary>
        public IList<TDSColumnData> Columns { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSColMetadataToken()
        {
            // Prepare collection
            Columns = new List<TDSColumnData>();
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Read column count
            ushort count = TDSUtilities.ReadUShort(source);

            // Check if count indicates that no columns are available
            if (count == 0xFFFF)
            {
                // We're done
                return true;
            }

            // Allocate new collection
            Columns = new List<TDSColumnData>();

            // Inflate each column
            for (ushort usIndex = 0; usIndex < count; usIndex++)
            {
                // Create a new column data
                TDSColumnData data = new TDSColumnData();

                // Inflate
                data.Inflate(source);

                // Register data with the collection
                Columns.Add(data);
            }

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.ColumnMetadata);

            // Check if there's any metadata
            if (Columns != null && Columns.Count > 0)
            {
                // Write column count
                TDSUtilities.WriteUShort(destination, (ushort)Columns.Count);

                // Iterate through each column and deflate it
                foreach (TDSColumnData column in Columns)
                {
                    // Deflate each column
                    column.Deflate(destination);
                }
            }
            else
            {
                // Indicate that there's no metadata
                TDSUtilities.WriteUShort(destination, (ushort)0xFFFF);
            }
        }
    }
}
