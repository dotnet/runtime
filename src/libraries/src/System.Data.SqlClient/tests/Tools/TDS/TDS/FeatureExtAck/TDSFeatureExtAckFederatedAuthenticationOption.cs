// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.SqlServer.TDS.SessionState;

namespace Microsoft.SqlServer.TDS.FeatureExtAck
{
    /// <summary>
    /// Acknowledgement for federated authentication
    /// </summary>
    public class TDSFeatureExtAckFederatedAuthenticationOption : TDSFeatureExtAckOption
    {
        /// <summary>
        /// Fixed Length of Nonce
        /// </summary>
        private static readonly uint s_nonceDataLength = 32;

        /// <summary>
        /// Fixed Length of Signature
        /// </summary>
        private static readonly uint s_signatureDataLength = 32;

        /// <summary>
        /// Signed nonce
        /// </summary>
        public byte[] ClientNonce { get; set; }

        /// <summary>
        /// The HMAC-SHA-256 [RFC6234] of the server-specified nonce
        /// </summary>
        public byte[] Signature { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSFeatureExtAckFederatedAuthenticationOption(byte[] clientNonce, byte[] signature) :
            this()
        {
            // Nonce and/or Signature can be null depending on the FedAuthLibrary used
            if (clientNonce == null && signature != null)
            {
                throw new ArgumentNullException("signature");
            }
            else if (clientNonce != null && clientNonce.Length != s_nonceDataLength)
            {
                throw new ArgumentOutOfRangeException("nonce");
            }
            else if (signature != null && signature.Length != s_signatureDataLength)
            {
                throw new ArgumentOutOfRangeException("signature");
            }

            // Save nonce
            ClientNonce = clientNonce;

            // Save signature
            Signature = signature;
        }

        /// <summary>
        /// Inflation constructor
        /// </summary>
        public TDSFeatureExtAckFederatedAuthenticationOption(Stream source) :
            this()
        {
            // Inflate
            Inflate(source);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private TDSFeatureExtAckFederatedAuthenticationOption()
        {
            // Set feature identifier
            FeatureID = TDSFeatureID.FederatedAuthentication;
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public override void Deflate(Stream destination)
        {
            // Write feature extension acknowledgement
            destination.WriteByte((byte)TDSFeatureID.FederatedAuthentication);

            // Write the data length
            TDSUtilities.WriteUInt(destination, ((ClientNonce != null) ? s_nonceDataLength : 0) + ((Signature != null) ? s_signatureDataLength : 0));

            if (ClientNonce != null)
            {
                // Write the Nonce            
                destination.Write(ClientNonce, 0, (int)s_nonceDataLength);
            }

            if (Signature != null)
            {
                // Write the signature
                destination.Write(Signature, 0, (int)s_signatureDataLength);
            }
        }

        /// <summary>
        /// Inflate from stream
        /// </summary>
        public override bool Inflate(Stream source)
        {
            // We skip feature ID because it was read by construction factory

            // Read the data length.
            uint dataLength = TDSUtilities.ReadUInt(source);

            if (dataLength > 0)
            {
                // Allocate a container
                ClientNonce = new byte[s_nonceDataLength];

                // Read the data
                source.Read(ClientNonce, 0, (int)s_nonceDataLength);
            }

            if (dataLength > s_nonceDataLength)
            {
                // Allocate Signature
                Signature = new byte[s_signatureDataLength];

                // Read the data
                source.Read(Signature, 0, (int)s_signatureDataLength);
            }

            // Inflation is complete
            return true;
        }
    }
}