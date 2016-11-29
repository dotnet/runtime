// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Token that contains session state
    /// </summary>
    public class TDSSessionStateToken : TDSPacketToken
    {
        /// <summary>
        /// Indicates that session is recoverable
        /// </summary>
        public bool IsRecoverable { get; set; }

        /// <summary>
        /// Sequential number of the state
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// State options
        /// </summary>
        public IList<TDSSessionStateOption> Options { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSessionStateToken()
        {
            Options = new List<TDSSessionStateOption>();
        }

        /// <summary>
        /// Inflate the token
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        public override bool Inflate(Stream source)
        {
            // We skip the token identifier because it is read by token factory

            // Current inflation offset
            uint inflationOffset = 0;

            // Read the length of the token
            uint tokenLength = TDSUtilities.ReadUInt(source);

            // NOTE: Length is excluded from the token length

            // Check if we still have space left to read
            if (inflationOffset >= tokenLength)
            {
                // We read the whole token
                return true;
            }

            // Read sequence number
            SequenceNumber = (int)TDSUtilities.ReadUInt(source);

            // Update inflation offset
            inflationOffset += sizeof(uint);

            // Check if we still have space left to read
            if (inflationOffset >= tokenLength)
            {
                // We read the whole token
                return true;
            }

            // Read status
            byte status = (byte)source.ReadByte();

            // Update inflation offset
            inflationOffset += sizeof(byte);

            // Check if we still have space left to read
            if (inflationOffset >= tokenLength)
            {
                // We read the whole token
                return true;
            }

            // Parse status
            IsRecoverable = ((status & 0x01) != 0);

            // Read while we have data
            while (tokenLength > inflationOffset)
            {
                // Read a byte that identifies the session state slot
                byte stateID = (byte)source.ReadByte();

                // Update current position
                inflationOffset += sizeof(byte);

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
                inflationOffset += option.InflationSize;
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
            destination.WriteByte((byte)TDSTokenType.SessionState);

            // Allocate a temporary memory stream
            MemoryStream memoryStream = new MemoryStream();

            // Iterate through all option
            foreach (TDSSessionStateOption option in Options)
            {
                // Deflate into the memory stream to calcualte the overall length
                option.Deflate(memoryStream);
            }

            // Write the length
            TDSUtilities.WriteUInt(destination, (uint)(memoryStream.Length + sizeof(uint) /* Sequence Number */ + sizeof(byte) /* Status */ + memoryStream.Length));

            // Write sequence number
            TDSUtilities.WriteUInt(destination, (uint)SequenceNumber);

            // Write status
            destination.WriteByte((byte)(IsRecoverable ? 1 : 0));

            // Write the options
            memoryStream.WriteTo(destination);
        }
    }
}
