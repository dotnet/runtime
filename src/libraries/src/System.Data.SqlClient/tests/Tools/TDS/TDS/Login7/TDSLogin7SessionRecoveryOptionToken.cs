// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

using Microsoft.SqlServer.TDS.SessionState;
using Microsoft.SqlServer.TDS.PreLogin;

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Token that is sent in the login packet for session recovery
    /// </summary>
    public class TDSLogin7SessionRecoveryOptionToken : TDSLogin7FeatureOptionToken
    {
        /// <summary>
        /// Feature type
        /// </summary>
        public override TDSFeatureID FeatureID { get { return TDSFeatureID.SessionRecovery; } }

        /// <summary>
        /// Initial state
        /// </summary>
        public TDSSessionRecoveryData Initial { get; set; }

        /// <summary>
        /// Current state
        /// </summary>
        public TDSSessionRecoveryData Current { get; set; }

        /// <summary>
        /// Initialization Constructor.
        /// </summary>
        public TDSLogin7SessionRecoveryOptionToken()
        {
        }

        /// <summary>
        /// Inflating constructor
        /// </summary>		
        public TDSLogin7SessionRecoveryOptionToken(Stream source) :
            this()
        {
            // Inflate feature extension data
            Inflate(source);
        }

        /// <summary>
        /// Inflate the token
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>        
        public override bool Inflate(Stream source)
        {
            // Reset inflation size
            InflationSize = 0;

            // We skip option identifier because it was read by construction factory

            // Read the length of the data for the option
            uint optionDataLength = TDSUtilities.ReadUInt(source);

            // Update inflation offset
            InflationSize += sizeof(uint);

            // Check if we still have space to read
            if (InflationSize >= optionDataLength)
            {
                // Inflation is complete
                return true;
            }

            // Inflate initial data set
            Initial = new TDSSessionRecoveryData(source);

            // Update inflation size with initial data set
            InflationSize += Initial.InflationSize;

            // Check if we still have space to read
            if (InflationSize >= optionDataLength)
            {
                // Inflation is complete
                return true;
            }

            // Inflate delta data set
            Current = new TDSSessionRecoveryData(source);

            // Update inflation size with initial data set
            InflationSize += Current.InflationSize;

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write option identifier
            destination.WriteByte((byte)FeatureID);

            // Deflate the token into the memory cache to calculate the length
            MemoryStream cache = new MemoryStream();

            // Check if we have an initial state
            if (Initial != null)
            {
                // Deflate initial state
                Initial.Deflate(cache);
            }

            // Check if we have a delta state
            if (Current != null)
            {
                // Deflate initial state
                Current.Deflate(cache);
            }

            // Write the cache length into the destination
            TDSUtilities.WriteUInt(destination, (uint)cache.Length);

            // Write cache itself
            cache.WriteTo(destination);
        }
    }
}
