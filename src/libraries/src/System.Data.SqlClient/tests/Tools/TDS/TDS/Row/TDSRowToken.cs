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
    /// Token that corresponds to the row of data
    /// </summary>
    public class TDSRowToken : TDSRowTokenBase
    {
        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSRowToken(TDSColMetadataToken metadata) :
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
            // Process each column
            foreach (TDSColumnData column in Metadata.Columns)
            {
                // Inflate and add data to the list
                Data.Add(InflateColumn(source, column));
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
            destination.WriteByte((byte)TDSTokenType.Row);

            // Process each column
            for (int index = 0; index < Metadata.Columns.Count; index++)
            {
                // Deflate each column
                DeflateColumn(destination, Metadata.Columns[index], Data[index]);
            }
        }
    }
}
