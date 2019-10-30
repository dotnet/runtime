// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

using Microsoft.SqlServer.TDS.Login7;

namespace Microsoft.SqlServer.TDS.LoginAck
{
    /// <summary>
    /// Login acknowledgement packet
    /// </summary>
    public class TDSLoginAckToken : TDSPacketToken
    {
        /// <summary>
        /// TDS Version used by the server
        /// </summary>
        public Version TDSVersion { get; set; }

        /// <summary>
        /// The type of interface with which the server will accept client requests
        /// </summary>
        public TDSLogin7TypeFlagsSQL Interface { get; set; }

        /// <summary>
        /// Name of the server (e.g. "Microsoft SQL Server")
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Server version
        /// </summary>
        public Version ServerVersion { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLoginAckToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLoginAckToken(Version serverVersion)
        {
            ServerVersion = serverVersion;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLoginAckToken(Version serverVersion, Version tdsVersion) :
            this(serverVersion)
        {
            TDSVersion = tdsVersion;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLoginAckToken(Version serverVersion, Version tdsVersion, TDSLogin7TypeFlagsSQL interfaceFlags) :
            this(serverVersion, tdsVersion)
        {
            Interface = interfaceFlags;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSLoginAckToken(Version serverVersion, Version tdsVersion, TDSLogin7TypeFlagsSQL interfaceFlags, string serverName) :
            this(serverVersion, tdsVersion, interfaceFlags)
        {
            ServerName = serverName;
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        public override bool Inflate(Stream source)
        {
            // We skip the token identifier because it is read by token factory

            // Read token length
            ushort tokenLength = (ushort)(source.ReadByte() + (source.ReadByte() << 8));

            // Read interface
            Interface = (TDSLogin7TypeFlagsSQL)source.ReadByte();

            // Read TDS version
            string tdsVersion = string.Format("{0:X}", (uint)(source.ReadByte() << 24)
                + (uint)(source.ReadByte() << 16)
                + (uint)(source.ReadByte() << 8)
                + (uint)(source.ReadByte()));

            // Consturct TDS version
            TDSVersion = new Version(int.Parse(tdsVersion.Substring(0, 1)), int.Parse(tdsVersion.Substring(1, 1)), Convert.ToInt32(tdsVersion.Substring(2, 2), 16), Convert.ToInt32(tdsVersion.Substring(4, 4), 16));

            // Read server name length
            byte serverNameLength = (byte)source.ReadByte();

            // Allocate buffer for server name
            byte[] serverNameBytes = new byte[serverNameLength * 2];

            // Read data into the buffer
            source.Read(serverNameBytes, 0, serverNameBytes.Length);

            // Convert to string
            ServerName = Encoding.Unicode.GetString(serverNameBytes);

            // Read server version
            ServerVersion = new Version(source.ReadByte(), source.ReadByte(), (source.ReadByte() << 8) + source.ReadByte());

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.LoginAcknowledgement);

            // Calculate the length of the token
            // The total length, in bytes, of the following fields: Interface, TDSVersion, Progname, and ProgVersion.
            ushort tokenLength = (ushort)(sizeof(byte) + sizeof(uint) + sizeof(byte) + (string.IsNullOrEmpty(ServerName) ? 0 : ServerName.Length * 2) + sizeof(uint));

            // Write the length
            destination.WriteByte((byte)(tokenLength & 0xff));
            destination.WriteByte((byte)((tokenLength >> 8) & 0xff));

            // Write interface
            destination.WriteByte((byte)Interface);

            // Compile TDS version
            uint tdsVersion = Convert.ToUInt32(string.Format("{0:X}", Math.Max(TDSVersion.Major, 0)) + string.Format("{0:X}", Math.Max(TDSVersion.Minor, 0)) + string.Format("{0:X2}", Math.Max(TDSVersion.Build, 0)) + string.Format("{0:X4}", Math.Max(TDSVersion.Revision, 0)), 16);

            // Write TDS version
            destination.WriteByte((byte)((tdsVersion >> 24) & 0xff));
            destination.WriteByte((byte)((tdsVersion >> 16) & 0xff));
            destination.WriteByte((byte)((tdsVersion >> 8) & 0xff));
            destination.WriteByte((byte)(tdsVersion & 0xff));

            // Write length of the server name
            destination.WriteByte((byte)(string.IsNullOrEmpty(ServerName) ? 0 : ServerName.Length));

            // Convert server name into byte stream
            byte[] serverNameBytes = Encoding.Unicode.GetBytes(ServerName);

            // Write server name
            destination.Write(serverNameBytes, 0, serverNameBytes.Length);

            // Write server version
            destination.WriteByte((byte)(ServerVersion.Major & 0xff));
            destination.WriteByte((byte)(ServerVersion.Minor & 0xff));
            destination.WriteByte((byte)((ServerVersion.Build >> 8) & 0xff));
            destination.WriteByte((byte)(ServerVersion.Build & 0xff));
        }
    }
}
