// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Container for session recovery data
    /// </summary>
    public class TDSSessionRecoveryData : IInflatable, IDeflatable
    {
        /// <summary>
        /// Size of the data read during inflation operation. It is needed to properly parse the option stream.
        /// </summary>
        internal uint InflationSize { get; set; }

        /// <summary>
        /// Database into which the user is logged in
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Collation of the database
        /// </summary>
        public byte[] Collation { get; set; }

        /// <summary>
        /// Language of the session
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Options that carry session state
        /// </summary>
        public IList<TDSSessionStateOption> Options { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSessionRecoveryData()
        {
        }

        /// <summary>
        /// Inflation constructor
        /// </summary>
        public TDSSessionRecoveryData(Stream source)
        {
            Inflate(source);
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public virtual void Deflate(Stream destination)
        {
            // Allocate temporary memory stream
            MemoryStream cache = new MemoryStream();

            // Write database
            cache.WriteByte((byte)(string.IsNullOrEmpty(Database) ? 0 : Database.Length));
            TDSUtilities.WriteString(cache, Database);

            // Write collation
            if (Collation == null)
            {
                // We don't have a collation
                cache.WriteByte(0);
            }
            else
            {
                // Write collation length
                cache.WriteByte((byte)Collation.Length);

                // Write it
                cache.Write(Collation, 0, Collation.Length);
            }

            // Write language
            cache.WriteByte((byte)(string.IsNullOrEmpty(Language) ? 0 : Language.Length));
            TDSUtilities.WriteString(cache, Language);

            // Check if we have options
            if (Options != null)
            {
                // Iterate through all options and deflate them
                foreach (TDSSessionStateOption option in Options)
                {
                    // Deflate into the cache to calcualte the overall length
                    option.Deflate(cache);
                }
            }

            // Write the length
            TDSUtilities.WriteUInt(destination, (uint)cache.Length);

            // Write the data itself
            cache.WriteTo(destination);
        }

        /// <summary>
        /// Inflate from stream
        /// </summary>
        public virtual bool Inflate(Stream source)
        {
            // Create options collection
            Options = new List<TDSSessionStateOption>();

            // Current position in the stream
            InflationSize = 0;

            // Read the total length
            uint totalLength = TDSUtilities.ReadUInt(source);

            // Check if we still have space to read
            if (InflationSize >= totalLength)
            {
                // Inflation is complete
                return true;
            }

            // Read the length of the string
            byte byteLength = (byte)source.ReadByte();

            // Update offset
            InflationSize += sizeof(byte);

            // Check if we still have space to read
            if (InflationSize >= totalLength)
            {
                // Inflation is complete
                return true;
            }

            // Read the string
            Database = TDSUtilities.ReadString(source, (ushort)(byteLength * 2));

            // Update offset
            InflationSize += ((uint)byteLength * 2);  // one character is 2 bytes long

            // Check if we still have space to read
            if (InflationSize >= totalLength)
            {
                // Inflation is complete
                return true;
            }

            // Read the length of the collation
            byteLength = (byte)source.ReadByte();

            // Update offset
            InflationSize += sizeof(byte);

            // Check if we still have space to read
            if (InflationSize >= totalLength)
            {
                // Inflation is complete
                return true;
            }

            // Check if we have a collation
            if (byteLength > 0)
            {
                // Allocate collation
                Collation = new byte[5];

                // Read collation
                int readBytes = source.Read(Collation, 0, Collation.Length);

                // Update offset
                InflationSize += (uint)readBytes;

                // Check if we still have space to read
                if (InflationSize >= totalLength)
                {
                    // Inflation is complete
                    return true;
                }
            }

            // Read the length of the string
            byteLength = (byte)source.ReadByte();

            // Update offset
            InflationSize += sizeof(byte);

            // Check if we still have space to read
            if (InflationSize >= totalLength)
            {
                // Inflation is complete
                return true;
            }

            // Read the string
            Language = TDSUtilities.ReadString(source, (ushort)(byteLength * 2));

            // Update offset
            InflationSize += ((uint)byteLength * 2);  // one character is 2 bytes long

            // Read while we have data
            while (totalLength > InflationSize)
            {
                // Read a byte that identifies the session state slot
                byte stateID = (byte)source.ReadByte();

                // Update current position
                InflationSize += sizeof(byte);

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
                InflationSize += option.InflationSize;
            }

            // Inflation is complete
            return true;
        }
    }
}
