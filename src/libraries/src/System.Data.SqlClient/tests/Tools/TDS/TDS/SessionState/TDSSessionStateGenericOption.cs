// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Generic session state option
    /// </summary>
    public class TDSSessionStateGenericOption : TDSSessionStateOption
    {
        /// <summary>
        /// State option value
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSSessionStateGenericOption(byte stateID) :
            base(stateID)
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSSessionStateGenericOption(byte stateID, byte[] value) :
            this(stateID)
        {
            Value = value;
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public override void Deflate(Stream destination)
        {
            // Write state ID
            destination.WriteByte(StateID);

            // Write value as is
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

            // Inflate value
            Value = InflateValue(source);

            // Inflation is complete
            return true;
        }
    }
}
