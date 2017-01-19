// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

using Microsoft.SqlServer.TDS.FeatureExtAck;
using Microsoft.SqlServer.TDS.PreLogin;

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Option token that can contain any data and doesn't have specialized inflation/deflation logic
    /// </summary>
    public class TDSLogin7GenericOptionToken : TDSLogin7FeatureOptionToken
    {
        /// <summary>
        /// Data that the token is carrying
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLogin7GenericOptionToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLogin7GenericOptionToken(TDSFeatureID featureID)
        {
            // Save feature identifier
            FeatureID = featureID;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLogin7GenericOptionToken(TDSFeatureID featureID, byte[] data) :
            this(featureID)
        {
            // Save data
            Data = data;
        }

        /// <summary>
        /// Inflating constructor
        /// </summary>		
        public TDSLogin7GenericOptionToken(Stream source) :
            this()
        {
            // Inflate feature extension data
            Inflate(source);
        }


        /// <summary>
        /// Inflate the Feature option
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Reset inflation size
            InflationSize = 0;

            // Skip feature ID inflation because it was read by options collection

            // Read the length
            int length = (int)TDSUtilities.ReadUInt(source);

            // Update inflation size
            InflationSize += sizeof(int);

            // Allocate a container for the specified length
            Data = new byte[length];

            // Read the data
            source.Read(Data, 0, Data.Length);

            // Update inflation size
            InflationSize += (uint)length;

            // We've inflated the token option
            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream the token to deflate to.</param>
        public override void Deflate(Stream destination)
        {
            // Write feature ID
            destination.WriteByte((byte)FeatureID);

            // Check if feature data is available
            if (Data == null)
            {
                // Length is zero
                TDSUtilities.WriteUInt(destination, 0);
            }
            else
            {
                // Write the length
                TDSUtilities.WriteUInt(destination, (uint)Data.Length);

                // Write the data itself
                destination.Write(Data, 0, Data.Length);
            }
        }
    }
}
