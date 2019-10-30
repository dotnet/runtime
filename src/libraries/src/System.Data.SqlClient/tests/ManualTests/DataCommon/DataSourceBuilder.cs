// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class DataSourceBuilder
    {
        public DataSourceBuilder(string dataSource)
        {
            // Extract port number
            string[] commaSplit = dataSource.Split(',');

            if (commaSplit.Length == 2)
            {
                // If the string cannot be converted to integer that indicates a user error and
                // we want that exception to bubble up. Hence using Parse instead of TryParse.
                Port = int.Parse(commaSplit[1].Trim());
            }
            else if (commaSplit.Length > 2)
            {
                throw new ArgumentException(string.Format("commaSplit of string {0} has length {1}", dataSource, commaSplit.Length));
            }

            // Extract protocol and server name
            string ProtocolAndServerName = commaSplit[0].Trim();
            string[] colonSplit = ProtocolAndServerName.Split(':');

            if (colonSplit.Length == 1)
            {
                ServerName = colonSplit[0];
            }
            else if (colonSplit.Length == 2)
            {
                Protocol = colonSplit[0];
                ServerName = colonSplit[1];
            }
            else
            {
                throw new ArgumentException(string.Format("colonSplit of string {0} has length {1}", ProtocolAndServerName, colonSplit.Length));
            }
        }

        public string Protocol { get; set; }

        public string ServerName { get; set; }

        public int? Port { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();

            if (Protocol != null)
            {
                b.Append(Protocol);
                b.Append(":");
            }

            b.Append(ServerName);

            if (Port != null)
            {
                b.Append(",");
                b.Append(Port);
            }

            return b.ToString();
        }
    }
}
