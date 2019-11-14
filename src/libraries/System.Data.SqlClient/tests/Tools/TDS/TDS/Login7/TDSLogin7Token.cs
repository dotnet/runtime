// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TDS.FeatureExtAck;

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Login 7 request packet
    /// </summary>
    public class TDSLogin7Token : TDSPacketToken
    {
        /// <summary>
        /// Length of the fixed portion of the packet
        /// </summary>
        protected static ushort FixedPacketLength = sizeof(uint)  // Length
                + sizeof(uint)  // TDSVersion
                + sizeof(uint)  // PacketSize
                + sizeof(uint)  // ClientProgramVersion
                + sizeof(uint)  // ClientPID
                + sizeof(uint)  // ConnectionID
                + sizeof(byte)  // OptionalFlags1
                + sizeof(byte)  // OptionalFlags2
                + sizeof(byte)  // OptionalFlags3
                + sizeof(byte)  // TypeFlags
                + sizeof(int)  // ClientTimeZone
                + sizeof(uint)  // ClientLCID
                + sizeof(ushort) + sizeof(ushort)  // HostName
                + sizeof(ushort) + sizeof(ushort)  // UserID
                + sizeof(ushort) + sizeof(ushort)  // Password
                + sizeof(ushort) + sizeof(ushort)  // ApplicationName
                + sizeof(ushort) + sizeof(ushort)  // ServerName
                + sizeof(ushort) + sizeof(ushort)  // Unused
                + sizeof(ushort) + sizeof(ushort)  // LibraryName
                + sizeof(ushort) + sizeof(ushort)  // Language
                + sizeof(ushort) + sizeof(ushort)  // Database
                + 6 * sizeof(byte)  // ClientID
                + sizeof(ushort) + sizeof(ushort)  // SSPI
                + sizeof(ushort) + sizeof(ushort)  // AttachDatabaseFile
                + sizeof(ushort) + sizeof(ushort)  // ChangePassword
                + sizeof(uint);  // LongSSPI;

        /// <summary>
        /// Version of the TDS protocol
        /// </summary>
        public Version TDSVersion { get; set; }

        /// <summary>
        /// Size of the TDS packet requested by the client
        /// </summary>
        public uint PacketSize { get; set; }

        /// <summary>
        /// Version of the client application
        /// </summary>
        public uint ClientProgramVersion { get; set; }

        /// <summary>
        /// Client application process identifier
        /// </summary>
        public uint ClientPID { get; set; }

        /// <summary>
        /// Connection identifier
        /// </summary>
        public uint ConnectionID { get; set; }

        /// <summary>
        /// First byte of optional flags
        /// </summary>
        public TDSLogin7TokenOptionalFlags1 OptionalFlags1 { get; set; }

        /// <summary>
        /// Second byte of optional flags
        /// </summary>
        public TDSLogin7TokenOptionalFlags2 OptionalFlags2 { get; set; }

        /// <summary>
        /// Third byte of optional flags
        /// </summary>
        public TDSLogin7TokenOptionalFlags3 OptionalFlags3 { get; set; }

        /// <summary>
        /// Type flags
        /// </summary>
        public TDSLogin7TokenTypeFlags TypeFlags { get; set; }

        /// <summary>
        /// Time zone of the client
        /// </summary>
        public int ClientTimeZone { get; set; }

        /// <summary>
        /// Client locale identifier
        /// </summary>
        public uint ClientLCID { get; set; }

        /// <summary>
        /// Client host name
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public string UserID { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Application name
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Client library name
        /// </summary>
        public string LibraryName { get; set; }

        /// <summary>
        /// User language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// User database
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Unique client identifier
        /// </summary>
        public byte[] ClientID { get; set; }

        /// <summary>
        /// Attach database file
        /// </summary>
        public string AttachDatabaseFile { get; set; }

        /// <summary>
        /// Change password
        /// </summary>
        public string ChangePassword { get; set; }

        /// <summary>
        /// SSPI authentication blob
        /// </summary>
        public byte[] SSPI { get; set; }

        /// <summary>
        /// Feature extension in the login7.
        /// </summary>
        public TDSLogin7FeatureOptionsToken FeatureExt { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSLogin7Token()
        {
            // Instantiate the first optional flags
            OptionalFlags1 = new TDSLogin7TokenOptionalFlags1();

            // Instantiate the second optional flags
            OptionalFlags2 = new TDSLogin7TokenOptionalFlags2();

            // Instantiate the third optional flags
            OptionalFlags3 = new TDSLogin7TokenOptionalFlags3();

            // Instantiate type flags
            TypeFlags = new TDSLogin7TokenTypeFlags();
        }

        /// <summary>
        /// Inflating constructor
        /// </summary>
        public TDSLogin7Token(Stream source)
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
            // Read packet length
            uint length = TDSUtilities.ReadUInt(source);

            // Read TDS version
            string tdsVersion = string.Format("{0:X}", TDSUtilities.ReadUInt(source));

            // Consturct TDS version
            TDSVersion = new Version(int.Parse(tdsVersion.Substring(0, 1)), int.Parse(tdsVersion.Substring(1, 1)), Convert.ToInt32(tdsVersion.Substring(2, 2), 16), Convert.ToInt32(tdsVersion.Substring(4, 4), 16));

            // Read packet length
            PacketSize = TDSUtilities.ReadUInt(source);

            // Read client program version
            ClientProgramVersion = TDSUtilities.ReadUInt(source);

            // Read client program identifier
            ClientPID = TDSUtilities.ReadUInt(source);

            // Read connection identifier
            ConnectionID = TDSUtilities.ReadUInt(source);

            // Instantiate the first optional flags
            OptionalFlags1 = new TDSLogin7TokenOptionalFlags1((byte)source.ReadByte());

            // Instantiate the second optional flags
            OptionalFlags2 = new TDSLogin7TokenOptionalFlags2((byte)source.ReadByte());

            // Instantiate type flags
            TypeFlags = new TDSLogin7TokenTypeFlags((byte)source.ReadByte());

            // Instantiate the third optional flags
            OptionalFlags3 = new TDSLogin7TokenOptionalFlags3((byte)source.ReadByte());

            // Read client time zone
            ClientTimeZone = TDSUtilities.ReadInt(source);

            // Read client locale identifier
            ClientLCID = TDSUtilities.ReadUInt(source);

            // Prepare a collection of property values that will be set later
            IList<TDSLogin7TokenOffsetProperty> variableProperties = new List<TDSLogin7TokenOffsetProperty>();

            // Read client host name
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("HostName"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read user name and password
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("UserID"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("Password"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read application name
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("ApplicationName"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read server name
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("ServerName"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Check if extension is used
            if (OptionalFlags3.ExtensionFlag)
            {
                // Read Feature extension. Note that this is just an offset of the value, not the value itself
                variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("FeatureExt"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source), true));
            }
            else
            {
                // Skip unused
                TDSUtilities.ReadUShort(source);
                TDSUtilities.ReadUShort(source);
            }

            // Read client library name
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("LibraryName"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read language
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("Language"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read database
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("Database"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            ClientID = new byte[6];

            // Read unique client identifier
            source.Read(ClientID, 0, ClientID.Length);

            // Read SSPI blob
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("SSPI"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read database file to be attached
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("AttachDatabaseFile"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read password change
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("ChangePassword"), TDSUtilities.ReadUShort(source), TDSUtilities.ReadUShort(source)));

            // Read long SSPI
            uint sspiLength = TDSUtilities.ReadUInt(source);

            // At this point we surpassed the fixed packet length
            long inflationOffset = FixedPacketLength;

            // Order strings in ascending order by offset
            // For the most cases this should not change the order of the options in the stream, but just in case
            variableProperties = variableProperties.OrderBy(p => p.Position).ToList();

            // We can't use "foreach" because FeatureExt processing changes the collection hence we can only go index-based way
            int iCurrentProperty = 0;

            // Iterate over each property
            while (iCurrentProperty < variableProperties.Count)
            {
                // Get the property at the indexed position
                TDSLogin7TokenOffsetProperty property = variableProperties[iCurrentProperty];

                // Check if length is positive
                if (property.Length == 0)
                {
                    // Move to the next propety
                    iCurrentProperty++;
                    continue;
                }

                // Ensure that current offset points to the option
                while (inflationOffset < property.Position)
                {
                    // Read the stream
                    source.ReadByte();

                    // Advance position
                    inflationOffset++;
                }

                // Check special properties
                if (property.Property.Name == "Password" || property.Property.Name == "ChangePassword")
                {
                    // Read passwod string
                    property.Property.SetValue(this, TDSUtilities.ReadPasswordString(source, (ushort)(property.Length * 2)), null);

                    // Advance the position
                    inflationOffset += (property.Length * 2);
                }
                else if (property.Property.Name == "SSPI")
                {
                    // If cbSSPI < USHRT_MAX, then this length MUST be used for SSPI and cbSSPILong MUST be ignored.
                    // If cbSSPI == USHRT_MAX, then cbSSPILong MUST be checked.
                    if (property.Length == ushort.MaxValue)
                    {
                        // If cbSSPILong > 0, then that value MUST be used. If cbSSPILong ==0, then cbSSPI (USHRT_MAX) MUST be used.
                        if (sspiLength > 0)
                        {
                            // We don't know how to handle SSPI packets that exceed TDS packet size
                            throw new NotSupportedException("Long SSPI blobs are not supported yet");
                        }
                    }

                    // Use short length instead
                    sspiLength = property.Length;

                    // Allocate buffer for SSPI data
                    SSPI = new byte[sspiLength];

                    // Read SSPI blob
                    source.Read(SSPI, 0, SSPI.Length);

                    // Advance the position
                    inflationOffset += sspiLength;
                }
                else if (property.Property.Name == "FeatureExt")
                {
                    // Check if this is the property or a pointer to the property
                    if (property.IsOffsetOffset)
                    {
                        // Read the actual offset of the feature extension
                        property.Position = TDSUtilities.ReadUInt(source);

                        // Mark that now we have actual value
                        property.IsOffsetOffset = false;

                        // Advance the position
                        inflationOffset += sizeof(uint);

                        // Re-order the collection
                        variableProperties = variableProperties.OrderBy(p => p.Position).ToList();

                        // Subtract position to stay on the same spot for subsequent property
                        iCurrentProperty--;
                    }
                    else
                    {
                        // Create a list of features.
                        FeatureExt = new TDSLogin7FeatureOptionsToken();

                        // Inflate feature extension
                        FeatureExt.Inflate(source);

                        // Advance position by the size of the inflated token
                        inflationOffset += FeatureExt.InflationSize;
                    }
                }
                else
                {
                    // Read the string and assign it to the property of this instance
                    property.Property.SetValue(this, TDSUtilities.ReadString(source, (ushort)(property.Length * 2)), null);

                    // Advance the position
                    inflationOffset += (property.Length * 2);
                }

                // Advance to the next property
                iCurrentProperty++;
            }

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Calculate total length by adding strings
            uint totalPacketLength = (uint)(FixedPacketLength
                + (uint)(string.IsNullOrEmpty(HostName) ? 0 : HostName.Length * 2)  // HostName
                + (uint)(string.IsNullOrEmpty(UserID) ? 0 : UserID.Length * 2)  // UserID
                + (uint)(string.IsNullOrEmpty(Password) ? 0 : Password.Length * 2)  // Password
                + (uint)(string.IsNullOrEmpty(ApplicationName) ? 0 : ApplicationName.Length * 2)  // ApplicationName
                + (uint)(string.IsNullOrEmpty(ServerName) ? 0 : ServerName.Length * 2)  // ServerName
                + (uint)(string.IsNullOrEmpty(LibraryName) ? 0 : LibraryName.Length * 2)  // LibraryName
                + (uint)(string.IsNullOrEmpty(Language) ? 0 : Language.Length * 2)  // Language
                + (uint)(string.IsNullOrEmpty(Database) ? 0 : Database.Length * 2)  // Database
                + (uint)(string.IsNullOrEmpty(AttachDatabaseFile) ? 0 : AttachDatabaseFile.Length * 2)  // AttachDatabaseFile
                + (uint)(string.IsNullOrEmpty(ChangePassword) ? 0 : ChangePassword.Length * 2)  // ChangePassword
                + (uint)(SSPI == null ? 0 : SSPI.Length)  // SSPI
                + 0);  // Feature extension

            MemoryStream featureExtension = null;

            // Check if we have a feature extension
            if (FeatureExt != null)
            {
                // Allocate feature extension block
                featureExtension = new MemoryStream();

                // Serialize feature extension
                FeatureExt.Deflate(featureExtension);

                // Update total lentgh
                totalPacketLength += (uint)(sizeof(uint) /* Offset of feature extension data */ + featureExtension.Length /* feature extension itself*/);
            }

            // Write packet length
            TDSUtilities.WriteUInt(destination, totalPacketLength);

            // Compile TDS version
            uint tdsVersion = Convert.ToUInt32(string.Format("{0:X}", Math.Max(TDSVersion.Major, 0)) + string.Format("{0:X}", Math.Max(TDSVersion.Minor, 0)) + string.Format("{0:X2}", Math.Max(TDSVersion.Build, 0)) + string.Format("{0:X4}", Math.Max(TDSVersion.Revision, 0)), 16);

            // Write TDS version
            TDSUtilities.WriteUInt(destination, tdsVersion);

            // Write packet length
            TDSUtilities.WriteUInt(destination, PacketSize);

            // Write client program version
            TDSUtilities.WriteUInt(destination, ClientProgramVersion);

            // Write client program identifier
            TDSUtilities.WriteUInt(destination, ClientPID);

            // Write connection identifier
            TDSUtilities.WriteUInt(destination, ConnectionID);

            // Write the first optional flags
            destination.WriteByte(OptionalFlags1.ToByte());

            // Write the second optional flags
            destination.WriteByte(OptionalFlags2.ToByte());

            // Instantiate type flags
            destination.WriteByte(TypeFlags.ToByte());

            // Write the third optional flags
            destination.WriteByte(OptionalFlags3.ToByte());

            // Write client time zone
            TDSUtilities.WriteInt(destination, ClientTimeZone);

            // Write client locale identifier
            TDSUtilities.WriteUInt(destination, ClientLCID);

            // Prepare a collection of property values that will be set later
            IList<TDSLogin7TokenOffsetProperty> variableProperties = new List<TDSLogin7TokenOffsetProperty>();

            // Write client host name
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("HostName"), FixedPacketLength, (ushort)(string.IsNullOrEmpty(HostName) ? 0 : HostName.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Write user name and password
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("UserID"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(UserID) ? 0 : UserID.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("Password"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(Password) ? 0 : Password.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Write application name
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("ApplicationName"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(ApplicationName) ? 0 : ApplicationName.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Write server name
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("ServerName"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(ServerName) ? 0 : ServerName.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Check if we have a feature extension block
            if (FeatureExt != null)
            {
                // Write the offset of the feature extension offset (pointer to pointer)
                variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("FeatureExt"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), sizeof(uint) / 2, true));  // Should be 4 bytes, devided by 2 because the next guy multiplies by 2
                TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
                TDSUtilities.WriteUShort(destination, (ushort)(variableProperties.Last().Length * 2));  // Compensate for division by 2 above
            }
            else
            {
                // Skip unused
                TDSUtilities.WriteUShort(destination, 0);
                TDSUtilities.WriteUShort(destination, 0);
            }

            // Write client library name
            // We do not need to account for skipped unused bytes here because they're already accounted in fixedPacketLength
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("LibraryName"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(LibraryName) ? 0 : LibraryName.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Write language
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("Language"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(Language) ? 0 : Language.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Write database
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("Database"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(Database) ? 0 : Database.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Check if client is defined
            if (ClientID == null)
            {
                // Allocate empty identifier
                ClientID = new byte[6];
            }

            // Write unique client identifier
            destination.Write(ClientID, 0, 6);

            // Write SSPI
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("SSPI"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(SSPI == null ? 0 : SSPI.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Write database file to be attached. NOTE, "variableProperties.Last().Length" without " * 2" because the preceeding buffer isn't string
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("AttachDatabaseFile"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length), (ushort)(string.IsNullOrEmpty(AttachDatabaseFile) ? 0 : AttachDatabaseFile.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Write password change
            variableProperties.Add(new TDSLogin7TokenOffsetProperty(GetType().GetProperty("ChangePassword"), (ushort)(variableProperties.Last().Position + variableProperties.Last().Length * 2), (ushort)(string.IsNullOrEmpty(ChangePassword) ? 0 : ChangePassword.Length)));
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Position);
            TDSUtilities.WriteUShort(destination, (ushort)variableProperties.Last().Length);

            // Skip long SSPI
            TDSUtilities.WriteUInt(destination, 0);

            // We will be changing collection as we go and serialize everything. As such we can't use foreach and iterator.
            int iCurrentProperty = 0;

            // Iterate through the collection
            while (iCurrentProperty < variableProperties.Count)
            {
                // Get current property by index
                TDSLogin7TokenOffsetProperty property = variableProperties[iCurrentProperty];

                // Check if length is positive
                if (property.Length == 0)
                {
                    // Move to the next property
                    iCurrentProperty++;
                    continue;
                }

                // Check special properties
                if (property.Property.Name == "Password" || property.Property.Name == "ChangePassword")
                {
                    // Write encrypted string value
                    TDSUtilities.WritePasswordString(destination, (string)property.Property.GetValue(this, null));
                }
                else if (property.Property.Name == "FeatureExt")
                {
                    // Check if we are to serialize the offset or the actual data
                    if (property.IsOffsetOffset)
                    {
                        // Property will be written at the offset immediately following all variable length data
                        property.Position = variableProperties.Last().Position + variableProperties.Last().Length;

                        // Write the position at which we'll be serializing the feature extension block
                        TDSUtilities.WriteUInt(destination, property.Position);

                        // Order strings in ascending order by offset
                        variableProperties = variableProperties.OrderBy(p => p.Position).ToList();

                        // Compensate increment to the next position in order to stay on the same
                        iCurrentProperty--;

                        // No longer offset, actual data is going to follow
                        property.IsOffsetOffset = false;
                    }
                    else
                    {
                        // Transfer deflated feature extension into the login stream
                        featureExtension.WriteTo(destination);
                    }
                }
                else if (property.Property.Name == "SSPI")
                {
                    // Write SSPI
                    destination.Write(SSPI, 0, SSPI.Length);
                }
                else
                {
                    // Write the string value
                    TDSUtilities.WriteString(destination, (string)property.Property.GetValue(this, null));
                }

                // Move to the next property
                iCurrentProperty++;
            }
        }
    }
}
