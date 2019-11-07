// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Session state for the deadlock priority
    /// </summary>
    public class TDSSessionStateDeadlockPriorityOption : TDSSessionStateOption
    {
        /// <summary>
        /// Identifier of the session state option
        /// </summary>
        public const byte ID = 4;

        /// <summary>
        /// Deadlock priority value
        /// </summary>
        public sbyte Value { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSessionStateDeadlockPriorityOption() :
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

            // Allocate a container
            byte[] value = new byte[1];

            // Put the date first
            value[0] = (byte)Value;

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

            // Read the first byte
            Value = (sbyte)value[0];

            // Inflation is complete
            return true;
        }
    }
}
