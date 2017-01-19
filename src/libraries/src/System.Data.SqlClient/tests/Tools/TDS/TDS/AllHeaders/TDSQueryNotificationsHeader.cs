// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.AllHeaders
{
    /// <summary>
    /// Represents Query Notifications header of ALL_HEADERS token
    /// </summary>
    public class TDSQueryNotificationsHeader : TDSPacketToken
    {
        /// <summary>
        /// User specified value when subscribing to the query notification
        /// </summary>
        public string NotifyID { get; set; }

        /// <summary>
        /// Service Broker Deployment
        /// </summary>
        public string SerciceBrokerDeployment { get; set; }

        /// <summary>
        /// Notification timeout
        /// </summary>
        public uint Timeout { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSQueryNotificationsHeader()
        {
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        public override bool Inflate(Stream source)
        {
            // Read the length of the notification ID
            ushort length = TDSUtilities.ReadUShort(source);

            // Read notification ID string
            NotifyID = TDSUtilities.ReadString(source, length);

            // Read the length of the service broker deployment
            length = TDSUtilities.ReadUShort(source);

            // Read service broker deployment string
            SerciceBrokerDeployment = TDSUtilities.ReadString(source, length);

            // Read query notification timeout
            Timeout = TDSUtilities.ReadUInt(source);

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Allocate temporary cache
            MemoryStream cache = new MemoryStream();

            // Write header type
            TDSUtilities.WriteUShort(cache, (ushort)TDSHeaderType.QueryNotifications);

            // Write notification ID string
            TDSUtilities.WriteUShort(cache, (ushort)(string.IsNullOrEmpty(NotifyID) ? 0 : NotifyID.Length));
            TDSUtilities.WriteString(cache, NotifyID);

            // Write service broker deployment
            TDSUtilities.WriteUShort(cache, (ushort)(string.IsNullOrEmpty(SerciceBrokerDeployment) ? 0 : SerciceBrokerDeployment.Length));
            TDSUtilities.WriteString(cache, SerciceBrokerDeployment);

            // Write timeout
            TDSUtilities.WriteUInt(cache, Timeout);

            // Write the header length including self into the destination
            TDSUtilities.WriteUInt(destination, (uint)(cache.Length + 4));

            // Write cached header data
            cache.WriteTo(destination);
        }
    }
}
