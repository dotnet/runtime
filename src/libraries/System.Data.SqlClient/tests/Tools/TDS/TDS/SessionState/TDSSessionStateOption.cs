// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// A single option of the session state
    /// </summary>
    public abstract class TDSSessionStateOption : IDeflatable, IInflatable
    {
        /// <summary>
        /// Property that tells the caller how much of the data from the stream was read by infation logic
        /// </summary>
        internal uint InflationSize { get; set; }

        /// <summary>
        /// Identifier of the state option
        /// </summary>
        public byte StateID { get; protected set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSSessionStateOption(byte stateID)
        {
            StateID = stateID;
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public abstract void Deflate(Stream destination);

        /// <summary>
        /// Inflate from stream
        /// </summary>
        public abstract bool Inflate(Stream source);

        /// <summary>
        /// Read the raw data but don't interpret it
        /// </summary>
        protected byte[] InflateValue(Stream source)
        {
            // Read the next byte to check if length is DWORD
            int length = (int)source.ReadByte();

            // Update inflation size
            InflationSize += sizeof(byte);

            // Check
            if (length == 0xFF)
            {
                // Length is long
                length = TDSUtilities.ReadInt(source);

                // Update inflation size
                InflationSize += sizeof(int);
            }

            // Allocate a container
            byte[] value = new byte[length];

            // Read all bytes
            source.Read(value, 0, value.Length);

            // Update inflation size
            InflationSize += (uint)length;

            return value;
        }

        /// <summary>
        /// Write raw data
        /// </summary>
        protected void DeflateValue(Stream destination, byte[] value)
        {
            // Check length
            if (value != null && value.Length >= 0xFF)
            {
                // Write the prefix to indicate a DWORD length
                destination.WriteByte(0xFF);

                // Length is DWORD
                TDSUtilities.WriteUInt(destination, (uint)value.Length);
            }
            else
            {
                // Length is byte
                destination.WriteByte((byte)value.Length);
            }

            // Serialize the data itself
            if (value != null)
            {
                destination.Write(value, 0, value.Length);
            }
        }
    }
}
