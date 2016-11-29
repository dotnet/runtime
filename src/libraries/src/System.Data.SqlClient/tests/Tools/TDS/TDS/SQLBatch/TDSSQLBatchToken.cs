// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

using Microsoft.SqlServer.TDS.AllHeaders;

namespace Microsoft.SqlServer.TDS.SQLBatch
{
    /// <summary>
    /// Login acknowledgement packet
    /// </summary>
    public class TDSSQLBatchToken : TDSPacketToken
    {
        /// <summary>
        /// All headers definition
        /// </summary>
        public TDSAllHeadersToken AllHeaders { get; set; }

        /// <summary>
        /// Query text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSQLBatchToken()
        {
        }

        /// <summary>
        /// Inflating constructor
        /// </summary>
        public TDSSQLBatchToken(Stream source)
        {
            Inflate(source);
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        public override bool Inflate(Stream source)
        {
            // Consturct all headers
            AllHeaders = new TDSAllHeadersToken();

            // Inflate all headers
            if (!AllHeaders.Inflate(source))
            {
                // Failed to inflate headers
                throw new ArgumentException("Failed to inflate all headers");
            }

            // Prepare memory stream
            MemoryStream cache = new MemoryStream();

            byte[] buffer = new byte[1024];
            int lastRead = 0;

            do
            {
                // Read the chunk of the stream into the memory stream
                lastRead = source.Read(buffer, 0, 1024);

                // Write into the memory stream
                cache.Write(buffer, 0, lastRead);
            }
            while (lastRead >= buffer.Length);

            // Read everything to the end of the stream
            Text = Encoding.Unicode.GetString(cache.ToArray());

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Check if we have all headers
            if (AllHeaders != null)
            {
                // Deflate all headers
                AllHeaders.Deflate(destination);
            }

            // Convert text to stream
            byte[] textBytes = Encoding.Unicode.GetBytes(Text);

            // Write to the destination
            destination.Write(textBytes, 0, textBytes.Length);
        }
    }
}
