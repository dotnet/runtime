// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;

using Microsoft.SqlServer.TDS.SessionState;

namespace Microsoft.SqlServer.TDS.FeatureExtAck
{
    /// <summary>
    /// Acknowledgement for session state recovery
    /// </summary>
    public class TDSFeatureExtAckSessionStateOption : TDSFeatureExtAckOption
    {
        /// <summary>
        /// Options that carry session state
        /// </summary>
        public IList<TDSSessionStateOption> Options { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSFeatureExtAckSessionStateOption()
        {
            // Set feature identifier
            FeatureID = TDSFeatureID.SessionRecovery;

            // Create options collection
            Options = new List<TDSSessionStateOption>();
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSFeatureExtAckSessionStateOption(IList<TDSSessionStateOption> options)
        {
            // Set feature identifier
            FeatureID = TDSFeatureID.SessionRecovery;

            // Save options
            Options = options;
        }

        /// <summary>
        /// Inflation constructor
        /// </summary>
        public TDSFeatureExtAckSessionStateOption(Stream source) :
            this()
        {
            // Inflate
            Inflate(source);
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public override void Deflate(Stream destination)
        {
            // Write feature extension acknowledgement
            destination.WriteByte((byte)TDSFeatureID.SessionRecovery);

            // Allocate temporary memory stream
            MemoryStream memoryStream = new MemoryStream();

            // Iterate through all options and deflate them
            foreach (TDSSessionStateOption option in Options)
            {
                // Deflate into the cache to calcualte the overall length
                option.Deflate(memoryStream);
            }

            // Write the length
            TDSUtilities.WriteUInt(destination, (uint)memoryStream.Length);

            // Write the data itself
            memoryStream.WriteTo(destination);
        }

        /// <summary>
        /// Inflate from stream
        /// </summary>
        public override bool Inflate(Stream source)
        {
            // Create options collection
            Options = new List<TDSSessionStateOption>();

            // We skip feature ID because it was read by construction factory

            // Read the total length
            uint totalLength = TDSUtilities.ReadUInt(source);

            // Current position in the stream
            uint currentLength = 0;

            // Read while we have data
            while (totalLength > currentLength)
            {
                // Read a byte that identifies the session state slot
                byte stateID = (byte)source.ReadByte();

                // Update current position
                currentLength += sizeof(byte);

                // Option being inflated
                TDSSessionStateOption option = null;

                // Dispatch inflation based on the state
                switch (stateID)
                {
                    // UserOptionAll
                    case TDSSessionStateUserOptionsOption.ID:
                        {
                            // Create a new option
                            option = new TDSSessionStateUserOptionsOption();
                            break;
                        }
                    // DateFirstDateFormat
                    case TDSSessionStateDateFirstDateFormatOption.ID:
                        {
                            // Create a new option
                            option = new TDSSessionStateDateFirstDateFormatOption();
                            break;
                        }
                    // DbDeadlockPri
                    case TDSSessionStateDeadlockPriorityOption.ID:
                        {
                            // Create a new option
                            option = new TDSSessionStateDeadlockPriorityOption();
                            break;
                        }
                    // LockTimeout
                    case TDSSessionStateLockTimeoutOption.ID:
                        {
                            // Create a new option
                            option = new TDSSessionStateLockTimeoutOption();
                            break;
                        }
                    // IsoFips
                    case TDSSessionStateISOFipsOption.ID:
                        {
                            // Create a new option
                            option = new TDSSessionStateISOFipsOption();
                            break;
                        }
                    // TextSize
                    case TDSSessionStateTextSizeOption.ID:
                        {
                            // Create a new option
                            option = new TDSSessionStateTextSizeOption();
                            break;
                        }
                    // ContextInfo
                    case TDSSessionStateContextInfoOption.ID:
                        {
                            // Create a new option
                            option = new TDSSessionStateContextInfoOption();
                            break;
                        }
                    default:
                        {
                            // Create a new option
                            option = new TDSSessionStateGenericOption(stateID);
                            break;
                        }
                }

                // Inflate the option
                option.Inflate(source);

                // Register option with the collection
                Options.Add(option);

                // Update current length with the inflation size of the data
                currentLength += option.InflationSize;
            }

            // Inflation is complete
            return true;
        }
    }
}
