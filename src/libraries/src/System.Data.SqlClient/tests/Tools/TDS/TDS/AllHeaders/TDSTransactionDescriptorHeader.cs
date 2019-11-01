// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.AllHeaders
{
    /// <summary>
    /// Represents Transaction Descriptor header of ALL_HEADERS token
    /// </summary>
    public class TDSTransactionDescriptorHeader : TDSPacketToken
    {
        /// <summary>
        /// Identifier of the transaction
        /// </summary>
        public ulong TransactionDescriptor { get; set; }

        /// <summary>
        /// Outstanding request count
        /// </summary>
        public uint OutstandingRequestCount { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSTransactionDescriptorHeader()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSTransactionDescriptorHeader(ulong transactionDescriptor)
        {
            TransactionDescriptor = transactionDescriptor;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSTransactionDescriptorHeader(ulong transactionDescriptor, uint outstandingRequestCount)
        {
            TransactionDescriptor = transactionDescriptor;
            OutstandingRequestCount = outstandingRequestCount;
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        public override bool Inflate(Stream source)
        {
            // Read transaction descriptor
            TransactionDescriptor = TDSUtilities.ReadULong(source);

            // Read outstanding request count
            OutstandingRequestCount = TDSUtilities.ReadUInt(source);

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

            // Write header type
            TDSUtilities.WriteUShort(cache, (ushort)TDSHeaderType.TransactionDescriptor);

            // Write transaction descriptor
            TDSUtilities.WriteULong(cache, TransactionDescriptor);

            // Write outstanding request count
            TDSUtilities.WriteUInt(cache, OutstandingRequestCount);

            // Write the header length including self into the destination
            TDSUtilities.WriteUInt(destination, (uint)(cache.Length + 4));

            // Write cached header data
            cache.WriteTo(destination);
        }
    }
}
