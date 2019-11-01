// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Session state for the context information
    /// </summary>
    public class TDSSessionStateContextInfoOption : TDSSessionStateOption
    {
        /// <summary>
        /// Identifier of the session state option
        /// </summary>
        public const byte ID = 11;

        /// <summary>
        /// Context information
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSessionStateContextInfoOption() :
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

            // Store the value
            DeflateValue(destination, Value);
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
            Value = InflateValue(source);

            // Inflation is complete
            return true;
        }
    }
}
