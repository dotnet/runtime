// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Container for the SSPI handshake response to be sent to the other party
    /// </summary>
    public class SSPIResponse
    {
        /// <summary>
        /// Payload to proceed to the next step of authentcation
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Indicates whether this is the last payload and no further processing is needed
        /// </summary>
        public bool IsFinal { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SSPIResponse()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public SSPIResponse(byte[] payload)
        {
            Payload = payload;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public SSPIResponse(byte[] payload, bool isFinal) :
            this(payload)
        {
            IsFinal = isFinal;
        }
    }
}
