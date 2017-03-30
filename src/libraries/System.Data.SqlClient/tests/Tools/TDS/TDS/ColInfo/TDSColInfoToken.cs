// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.SqlServer.TDS.ColInfo
{
    /// <summary>
    /// Result set metadata description "COLINFO" token
    /// </summary>
    public class TDSColInfoToken : TDSPacketToken
    {
        /// <summary>
        /// A collection of columns for which metadata is available
        /// </summary>
        public IList<TDSColumnProperty> Columns { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSColInfoToken()
        {
            // Prepare collection
            Columns = new List<TDSColumnProperty>();
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Re-prepare collection
            Columns = new List<TDSColumnProperty>();

            // Read the length of the data
            ushort length = TDSUtilities.ReadUShort(source);

            // Current offset in the stream
            ushort offset = 0;

            // Inflate each property
            while (offset < length)
            {
                // Allocate a new property
                TDSColumnProperty newColumn = new TDSColumnProperty();

                // Inflate
                newColumn.Inflate(source);

                // Register with the collection
                Columns.Add(newColumn);

                // Update offset with the size of the last inflated property
                offset += (ushort)newColumn.InflationSize;
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
            destination.WriteByte((byte)TDSTokenType.ColumnInfo);

            // Allocate a memory stream for column information block
            MemoryStream cache = new MemoryStream();

            // Deflate all properties into the cache
            foreach (TDSColumnProperty column in Columns)
            {
                column.Deflate(cache);
            }

            // Write the length of the token
            TDSUtilities.WriteUShort(destination, (ushort)cache.Length);

            // Write the cache itself
            cache.WriteTo(destination);
        }
    }
}
