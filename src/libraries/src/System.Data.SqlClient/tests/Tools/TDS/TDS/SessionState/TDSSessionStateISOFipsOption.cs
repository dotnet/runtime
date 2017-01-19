// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Session state for the transaction isolation level and FIPS 127-2 compliance flags
    /// </summary>
    public class TDSSessionStateISOFipsOption : TDSSessionStateOption
    {
        /// <summary>
        /// Identifier of the session state option
        /// </summary>
        public const byte ID = 7;

        /// <summary>
        /// Transaction isolation level
        /// </summary>
        public TransactionIsolationLevelType TransactionIsolationLevel { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSessionStateISOFipsOption() :
            base(ID) // State identifier
        {
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public override void Deflate(Stream destination)
        {
            // Write state ID
            destination.WriteByte(StateID);

            // Prepare container
            byte[] value = new byte[1];

            // Put transaction isolation level into it
            value[0] = (byte)(((byte)TransactionIsolationLevel) & 0xf);

            // Store the value
            DeflateValue(destination, value);
        }

        /// <summary>
        /// Inflate from stream
        /// </summary>
        public override bool Inflate(Stream source)
        {
            // Reset inflation size
            InflationSize = 0;

            // NOTE: state ID is skipped because it is read by the construction factory

            // Read the value
            byte[] value = InflateValue(source);

            // Get transaction isolation level
            TransactionIsolationLevel = (TransactionIsolationLevelType)((byte)(value[0] & 0xf));

            // Inflation is complete
            return true;
        }
    }
}
