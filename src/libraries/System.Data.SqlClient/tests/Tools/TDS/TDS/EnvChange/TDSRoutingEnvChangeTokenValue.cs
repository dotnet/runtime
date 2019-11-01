// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.EnvChange
{
    /// <summary>
    /// Token value that represents routing information
    /// </summary>
    public class TDSRoutingEnvChangeTokenValue : IInflatable, IDeflatable
    {
        /// <summary>
        /// Protocol to use when connecting to the target server
        /// </summary>
        public TDSRoutingEnvChangeTokenValueType Protocol { get; set; }

        /// <summary>
        /// Protocol details
        /// </summary>
        public object ProtocolProperty { get; set; }

        /// <summary>
        /// Location of the target server
        /// </summary>
        public string AlternateServer { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSRoutingEnvChangeTokenValue()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSRoutingEnvChangeTokenValue(TDSRoutingEnvChangeTokenValueType protocol)
        {
            // Save protocol
            Protocol = protocol;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSRoutingEnvChangeTokenValue(TDSRoutingEnvChangeTokenValueType protocol, object protocolProperty)
        {
            // Save protocol and protocol property
            Protocol = protocol;
            ProtocolProperty = protocolProperty;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSRoutingEnvChangeTokenValue(TDSRoutingEnvChangeTokenValueType protocol, object protocolProperty, string alternateServer)
        {
            // Save all
            Protocol = protocol;
            ProtocolProperty = protocolProperty;
            AlternateServer = alternateServer;
        }

        /// <summary>
        /// Inflate the token
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public virtual bool Inflate(Stream source)
        {
            // Read protocol value
            Protocol = (TDSRoutingEnvChangeTokenValueType)source.ReadByte();

            // Based on the protocol type read the rest of the token
            switch (Protocol)
            {
                case TDSRoutingEnvChangeTokenValueType.TCP:
                    {
                        // Read port
                        ProtocolProperty = TDSUtilities.ReadUShort(source);

                        // Read alternate server name of the length
                        AlternateServer = TDSUtilities.ReadString(source, (ushort)(TDSUtilities.ReadUShort(source) * 2));

                        break;
                    }
                default:
                    {
                        throw new Exception("Unrecognized routing protocol");
                    }
            }

            // Inflation is complete
            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public virtual void Deflate(Stream destination)
        {
            // Write protocol value
            destination.WriteByte((byte)Protocol);

            // Based on the protocol type read the rest of the token
            switch (Protocol)
            {
                default:
                case TDSRoutingEnvChangeTokenValueType.TCP:
                    {
                        // Write port
                        TDSUtilities.WriteUShort(destination, (ushort)ProtocolProperty);

                        // Write alternate server name length
                        TDSUtilities.WriteUShort(destination, (ushort)(string.IsNullOrEmpty(AlternateServer) ? 0 : AlternateServer.Length));

                        // Write alternate server name
                        TDSUtilities.WriteString(destination, AlternateServer);

                        break;
                    }
            }
        }

        /// <summary>
        /// Override string representation method
        /// </summary>
        public override string ToString()
        {
            return string.Format("Protocol: {0}; Protocol Property: {1}; Alternate Server: {2}", Protocol, ProtocolProperty, AlternateServer);
        }
    }
}
