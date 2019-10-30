// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.SqlServer.TDS.AllHeaders
{
    /// <summary>
    /// Token that handles ALL_HEADERS rule
    /// </summary>
    public class TDSAllHeadersToken : TDSPacketToken
    {
        /// <summary>
        /// Collection of individual headers
        /// </summary>
        public IList<TDSPacketToken> Headers { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSAllHeadersToken()
        {
            Headers = new List<TDSPacketToken>();
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        public override bool Inflate(Stream source)
        {
            // Allocate headers
            Headers = new List<TDSPacketToken>();

            // Read total length
            uint totalLength = TDSUtilities.ReadUInt(source);

            // Take total length DWORD into account
            totalLength -= 4;

            // Keep reading the stream until we inflate all headers
            while (totalLength > 0)
            {
                // Read header length
                uint headerLength = TDSUtilities.ReadUInt(source);

                // Update total length before we start modifying it
                totalLength -= headerLength;

                // Take header length DWORD into account
                headerLength -= 4;

                // Check if header length allows header type
                if (headerLength >= 2)
                {
                    // Read header type
                    TDSHeaderType type = (TDSHeaderType)TDSUtilities.ReadUShort(source);

                    // Take header type into account
                    headerLength -= 2;

                    // Based on the header type inflate the header
                    switch (type)
                    {
                        case TDSHeaderType.QueryNotifications:
                            {
                                // Create instance
                                Headers.Add(new TDSQueryNotificationsHeader());
                                break;
                            }
                        case TDSHeaderType.TransactionDescriptor:
                            {
                                // Create instance
                                Headers.Add(new TDSTransactionDescriptorHeader());
                                break;
                            }
                        case TDSHeaderType.Trace:
                            {
                                // Create instance
                                Headers.Add(new TDSTraceHeader());
                                break;
                            }
                        default:
                            {
                                // We don't know this token
                                throw new ArgumentException("Unrecognized header type", "Header Type");
                            }
                    }

                    // Inflate the last header
                    if (!Headers.Last().Inflate(source))
                    {
                        // We failed to inflate this token so we can't proceed
                        throw new Exception(string.Format("Failed to inflate header \"{0}\"", type));
                    }
                }
            }

            // We're done inflating
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

            // Check if headers are available
            if (Headers != null)
            {
                // Iterate through all headers
                foreach (TDSPacketToken header in Headers)
                {
                    // Deflate header into the cache
                    header.Deflate(cache);
                }
            }

            // Write the total length of cache including self into the destination
            TDSUtilities.WriteUInt(destination, (uint)(cache.Length + 4));

            // Seriaize the cache
            cache.WriteTo(destination);
        }
    }
}
