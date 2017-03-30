// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.SqlServer.TDS.PreLogin
{
    /// <summary>
    /// Pre-login packet
    /// </summary>
    public class TDSPreLoginToken : TDSPacketToken
    {
        /// <summary>
        /// Version of the sender
        /// Version: 4 bytes unsigned
        /// Build: 2 bytes unsigned
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Sub-build number of the sender.
        /// </summary>
        public ushort SubBuild { get; set; }

        /// <summary>
        /// Type of encryption
        /// 1 byte
        /// </summary>
        public TDSPreLoginTokenEncryptionType Encryption { get; set; }

        /// <summary>
        /// Thread identifier of the sender
        /// 4 bytes unsigned
        /// </summary>
        public uint ThreadID { get; set; }

        /// <summary>
        /// Indicates whether MARS is enabled on connection
        /// 1 byte
        /// </summary>
        public bool IsMARS { get; set; }

        /// <summary>
        /// Client Application trace ID
        /// </summary>
        public byte[] ClientTraceID { get; set; }

        /// <summary>
        /// Client application activity ID, used for debugging purposes.
        /// </summary>
        public byte[] ActivityID { get; set; }

        /// <summary>
        /// Nonce to be encrypted using session key from federated authentication key 
        /// from federated handshake.
        /// </summary>
        public byte[] Nonce { get; set; }

        /// <summary>
        /// Federated Authentication required for the pre-login.
        /// </summary>
        public TdsPreLoginFedAuthRequiredOption FedAuthRequired { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSPreLoginToken()
        {
            // Initialize thread identifier
            ThreadID = 0;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPreLoginToken(Version version)
        {
            // Save the version
            Version = version;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPreLoginToken(Version version, TDSPreLoginTokenEncryptionType encryption) :
            this(version)
        {
            // Save encryption setting
            Encryption = encryption;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPreLoginToken(Version version, TDSPreLoginTokenEncryptionType encryption, bool isMARS) :
            this(version, encryption)
        {
            // Save MARS
            IsMARS = isMARS;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPreLoginToken(Version version, TDSPreLoginTokenEncryptionType encryption, bool isMARS, uint threadID) :
            this(version, encryption, isMARS)
        {
            // Save thread ID
            ThreadID = threadID;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>		
        public TDSPreLoginToken(Version version, TDSPreLoginTokenEncryptionType encryption, bool isMARS, uint threadID, TdsPreLoginFedAuthRequiredOption fedAuthRequired) :
            this(version, encryption, isMARS, threadID)
        {
            FedAuthRequired = fedAuthRequired;
        }

        /// <summary>
        /// Inflating constructor
        /// </summary>
        public TDSPreLoginToken(Stream source)
        {
            // Inflate token
            Inflate(source);
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // Prepare a list of options
            IList<TDSPreLoginTokenOption> options = new List<TDSPreLoginTokenOption>();

            // Inflate all options until terminator is detected
            do
            {
                // Create a new option
                options.Add(new TDSPreLoginTokenOption());

                // Inflate it
                if (!options[options.Count - 1].Inflate(source))
                {
                    return false;
                }
            }
            while (options[options.Count - 1].Type != TDSPreLoginTokenOptionType.Terminator);

            // Order the options in ascending order by offset
            // For the most cases this should not change the order of the options in the stream, but just in case
            options = options.OrderBy(o => o.Position).ToList();

            // Calculate current inflation offset 
            ushort inflationOffset = (ushort)options.Sum(o => o.TokenLength);

            // Iterate through each option and inflate it
            foreach (TDSPreLoginTokenOption option in options)
            {
                // Ensure that current offset points to the option
                while (inflationOffset < option.Position)
                {
                    // Read the stream
                    source.ReadByte();

                    // Advance position
                    inflationOffset++;
                }

                // Check the type of the pre-login packet option type
                switch (option.Type)
                {
                    case TDSPreLoginTokenOptionType.Version:
                        {
                            // Check if version fits
                            if (option.Length >= 6)
                            {
                                // Read the data of the specified length at the specified position
                                Version = new Version(
                                    source.ReadByte() & 0xff, // Major
                                    source.ReadByte(), // Minor
                                    (source.ReadByte() << 8) + source.ReadByte());  // Build (swap bytes)

                                // Read sub-build
                                SubBuild = TDSUtilities.ReadUShort(source);

                                // Update the offset
                                inflationOffset += 6;
                            }

                            break;
                        }
                    case TDSPreLoginTokenOptionType.Encryption:
                        {
                            // Check is option fits
                            if (option.Length >= 1)
                            {
                                // Read encryption
                                Encryption = (TDSPreLoginTokenEncryptionType)source.ReadByte();

                                // Update the offset
                                inflationOffset += 1;
                            }

                            break;
                        }
                    case TDSPreLoginTokenOptionType.Instance:
                        {
                            // Currently does nothing.
                            break;
                        }
                    case TDSPreLoginTokenOptionType.ThreadID:
                        {
                            // Check if thread ID fits
                            if (option.Length >= 4)
                            {
                                // Read the data of the specified length at the specified position (big-endian)
                                ThreadID = TDSUtilities.ReadUInt(source);

                                // Update the offset
                                inflationOffset += 4;
                            }

                            break;
                        }
                    case TDSPreLoginTokenOptionType.Mars:
                        {
                            // Check is option fits
                            if (option.Length >= 1)
                            {
                                // Read byte
                                IsMARS = (source.ReadByte() == 0x01);

                                // Update the offset
                                inflationOffset += 1;
                            }

                            break;
                        }
                    case TDSPreLoginTokenOptionType.TraceID:
                        {
                            if (option.Length >= 36)
                            {
                                // Allocate memory
                                ClientTraceID = new byte[16];

                                // Read connection Trace ID
                                source.Read(ClientTraceID, 0, 16);

                                // Allocate memory
                                ActivityID = new byte[20];

                                // Read Activity ID.
                                source.Read(ActivityID, 0, 20);

                                // Update the offset
                                inflationOffset += 36;
                            }
                            break;
                        }
                    case TDSPreLoginTokenOptionType.FederatedAuthenticationRequired:
                        {
                            if (option.Length >= 1)
                            {
                                // Read authentication type.
                                FedAuthRequired = (TdsPreLoginFedAuthRequiredOption)source.ReadByte();

                                // Update the offset
                                inflationOffset += 1;
                            }
                            break;
                        }
                    case TDSPreLoginTokenOptionType.NonceOption:
                        {
                            if (option.Length >= 32)
                            {
                                //Allocate memory
                                Nonce = new byte[32];

                                // Read Nonce.
                                source.Read(Nonce, 0, 32);

                                // Update the offset
                                inflationOffset += 32;
                            }
                            break;
                        }
                }
            }

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            IList<TDSPreLoginTokenOption> options = new List<TDSPreLoginTokenOption>();

            // Prepare the sequence of options to serialize
            options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.Version, 6));
            options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.ThreadID, 4));
            options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.Mars, 1));

            //Check if the option is needed.
            if (ActivityID != null && ClientTraceID != null)
            {
                options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.TraceID, 36));
            }

            // Add Federated authentication
            if (FedAuthRequired == TdsPreLoginFedAuthRequiredOption.FedAuthRequired || FedAuthRequired == TdsPreLoginFedAuthRequiredOption.Illegal)
            {
                options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.FederatedAuthenticationRequired, 1));
            }

            // Check if the option is needed to be added.
            if (Nonce != null)
            {
                options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.NonceOption, (ushort)Nonce.Length));
            }

            options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.Encryption, 1));
            options.Add(new TDSPreLoginTokenOption(TDSPreLoginTokenOptionType.Terminator, 0));

            // Calculate the total size of the token metadata
            ushort dataOffset = (ushort)options.Sum(t => t.TokenLength);

            // Update all the offsets
            foreach (TDSPreLoginTokenOption option in options)
            {
                // Update token offset
                option.Position = dataOffset;

                // Update data offset
                dataOffset += option.Length;
            }

            // Serialize all tokens first
            foreach (TDSPreLoginTokenOption option in options)
            {
                // Deflate a token option
                option.Deflate(destination);
            }

            // Write version
            destination.WriteByte((byte)(Version.Major & 0xff));
            destination.WriteByte((byte)(Version.Minor & 0xff));
            destination.WriteByte((byte)((Version.Build >> 8) & 0xff));
            destination.WriteByte((byte)(Version.Build & 0xff));

            // Write sub-version
            TDSUtilities.WriteUShort(destination, SubBuild);

            // Write thread ID
            TDSUtilities.WriteUInt(destination, ThreadID);

            // Write MARS
            destination.WriteByte((byte)(IsMARS ? 0x01 : 0x00));

            // Write traceID
            if (ClientTraceID != null)
            {
                destination.Write(ClientTraceID, 0, ClientTraceID.Length);
            }

            // Write ActivityID
            if (ActivityID != null)
            {
                destination.Write(ActivityID, 0, ActivityID.Length);
            }

            // Write federated auth required part
            if (FedAuthRequired == TdsPreLoginFedAuthRequiredOption.FedAuthRequired || FedAuthRequired == TdsPreLoginFedAuthRequiredOption.Illegal)
            {
                destination.WriteByte((byte)FedAuthRequired);
            }

            // Write Nonce.
            if (Nonce != null)
            {
                destination.Write(Nonce, 0, Nonce.Length);
            }

            // Write Encryption
            destination.WriteByte((byte)Encryption);
        }
    }
}
