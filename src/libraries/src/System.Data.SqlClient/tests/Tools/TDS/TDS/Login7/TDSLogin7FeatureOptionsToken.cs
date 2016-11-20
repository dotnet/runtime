// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;

using Microsoft.SqlServer.TDS.FeatureExtAck;
using Microsoft.SqlServer.TDS.SessionState;

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Feature extension data delivered in the login packet
    /// </summary>
    public class TDSLogin7FeatureOptionsToken : List<TDSLogin7FeatureOptionToken>, IInflatable, IDeflatable
    {
        /// <summary>
        /// Property used internally by inflation/deflation routine to tell caller how much data was read/written to the stream
        /// </summary>
        internal uint InflationSize { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLogin7FeatureOptionsToken()
        {
        }

        /// <summary>
        /// Inflate an object instance from the stream
        /// </summary>
        public bool Inflate(Stream source)
        {
            // Identifier of the feature
            TDSFeatureID featureID = TDSFeatureID.Terminator;

            // Iterate
            do
            {
                // Read the feature type
                featureID = (TDSFeatureID)source.ReadByte();

                // Token being inflated
                TDSLogin7FeatureOptionToken optionToken = null;

                // skip this feature extension
                switch (featureID)
                {
                    case TDSFeatureID.FederatedAuthentication:
                        {
                            // Federated authentication
                            optionToken = new TDSLogin7FedAuthOptionToken();
                            break;
                        }
                    case TDSFeatureID.SessionRecovery:
                        {
                            // Session recovery
                            optionToken = new TDSLogin7SessionRecoveryOptionToken();
                            break;
                        }
                    case TDSFeatureID.Terminator:
                        {
                            // Do nothing
                            break;
                        }
                    default:
                        {
                            // Create a generic option
                            optionToken = new TDSLogin7GenericOptionToken(featureID);
                            break;
                        }
                }

                // Check if we have an option token
                if (optionToken != null)
                {
                    // Inflate it
                    optionToken.Inflate(source);

                    // Register with the collection
                    Add(optionToken);

                    // Update inflation offset
                    InflationSize += optionToken.InflationSize;
                }
            }
            while (TDSFeatureID.Terminator != featureID);

            // We don't support continuation of inflation so report as fully inflated
            return true;
        }

        /// <summary>
        /// Serialize object into the stream
        /// </summary>
        /// <param name="destination"></param>
        public void Deflate(Stream destination)
        {
            // Deflate each feature extension
            foreach (TDSLogin7FeatureOptionToken option in this)
            {
                option.Deflate(destination);
            }

            // Write the Terminator.
            destination.WriteByte((byte)TDSFeatureID.Terminator);
        }
    }
}
