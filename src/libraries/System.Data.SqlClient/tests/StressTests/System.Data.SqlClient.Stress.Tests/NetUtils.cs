// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Net;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Test.Data.SqlClient
{
    public static class NetUtils
    {
        // according to RFC 5737 (http://tools.ietf.org/html/rfc5737): The blocks 192.0.2.0/24 (TEST-NET-1), 198.51.100.0/24 (TEST-NET-2),
        // and 203.0.113.0/24 (TEST-NET-3) are provided for use in documentation and should not be in use by any public network
        private static readonly IPAddress[] s_testNets = new IPAddress[]
        {
            IPAddress.Parse("192.0.2.0"),
            IPAddress.Parse("198.51.100.0"),
            IPAddress.Parse("203.0.113.0")
        };

        private const int TestNetAddressRangeLength = 256;

        public static readonly int NonExistingIPv4AddressCount = TestNetAddressRangeLength * s_testNets.Length;

        public static IPAddress GetNonExistingIPv4(int index)
        {
            if (index < 0 || index > NonExistingIPv4AddressCount)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            byte[] address = s_testNets[index / TestNetAddressRangeLength].GetAddressBytes();

            Debug.Assert(address[3] == 0, "address ranges above must end with .0");
            address[3] = checked((byte)(index % TestNetAddressRangeLength));

            return new IPAddress(address);
        }

        public static IEnumerable<IPAddress> EnumerateIPv4Addresses(string hostName)
        {
            hostName = hostName.Trim();

            if ((hostName == ".") ||
                 (string.Compare("(local)", hostName, StringComparison.OrdinalIgnoreCase) == 0))
            {
                hostName = Dns.GetHostName();
            }

            Task<IPAddress[]> task = Dns.GetHostAddressesAsync(hostName);
            IPAddress[] allAddresses = task.Result;

            foreach (var addr in allAddresses)
            {
                if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    yield return addr;
                }
            }
        }

        /// <summary>
        /// Splits data source into protocol, host name, instance name and port.
        ///         
        /// Note that this algorithm does not cover all valid combinations of data source; only those we actually use in tests are supported now.
        /// Please update as needed.
        /// </summary>
        public static void ParseDataSource(string dataSource, out string protocol, out string hostName, out string instanceName, out int? port)
        {
            // check for protocol prefix
            int i = dataSource.IndexOf(':');
            if (i >= 0)
            {
                protocol = dataSource.Substring(0, i);

                // remove the protocol
                dataSource = dataSource.Substring(i + 1);
            }
            else
            {
                protocol = null;
            }

            // check for server port
            i = dataSource.IndexOf(',');
            if (i >= 0)
            {
                // there is a port value in connection string
                port = int.Parse(dataSource.Substring(i + 1));
                dataSource = dataSource.Substring(0, i);
            }
            else
            {
                port = null;
            }

            // check for the instance name
            i = dataSource.IndexOf('\\');
            if (i >= 0)
            {
                instanceName = dataSource.Substring(i + 1);
                dataSource = dataSource.Substring(0, i);
            }
            else
            {
                instanceName = null;
            }

            // trim redundant whitespace
            dataSource = dataSource.Trim();
            hostName = dataSource;
        }

        private static Dictionary<string, int> s_dataSourceToPortCache = new Dictionary<string, int>();

        /// <summary>
        /// the method converts the regular connection string to one supported by MultiSubnetFailover (connect to the port, bypassing the browser)
        /// it does the following:
        /// * removes Failover Partner, if presents
        /// * removes the network library and protocol prefix (only TCP is supported)
        /// * if instance name is specified without port value, data source is replaced with "server, port" format instead of "server\name"
        /// 
        /// Note that this method can create a connection to the server in case TCP port is needed. The port value is cached per data source, to avoid round trip to the server on next use.
        /// </summary>
        /// <param name="connectionString">original connection string, must be valid</param>
        /// <param name="replaceServerName">optionally, replace the (network) server name with a different one</param>
        /// <param name="originalServerName">holds the original server name on return</param>
        /// <returns>MultiSubnetFailover-enabled connection string builder</returns>
        public static SqlConnectionStringBuilder GetMultiSubnetFailoverConnectionString(string connectionString, string replaceServerName, out string originalServerName)
        {
            SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder(connectionString);

            sb["Network Library"] = null; // MSF supports TCP only, no need to specify the protocol explicitly
            sb["Failover Partner"] = null; // not supported, remove it if present

            string protocol, instance;
            int? serverPort;

            ParseDataSource(sb.DataSource, out protocol, out originalServerName, out instance, out serverPort);

            // Note: protocol value is ignored, connection to the server will fail if TCP is not enabled on the server

            if (!serverPort.HasValue)
            {
                // to get server listener's TCP port, connect to it using the original string, with TCP protocol enforced
                // to improve stress performance, cache the port value to avoid round trip every time new connection string is needed
                lock (s_dataSourceToPortCache)
                {
                    int cachedPort;
                    string cacheKey = sb.DataSource;
                    if (s_dataSourceToPortCache.TryGetValue(cacheKey, out cachedPort))
                    {
                        serverPort = cachedPort;
                    }
                    else
                    {
                        string originalServerNameWithInstance = sb.DataSource;
                        int protocolEndIndex = originalServerNameWithInstance.IndexOf(':');
                        if (protocolEndIndex >= 0)
                        {
                            originalServerNameWithInstance = originalServerNameWithInstance.Substring(protocolEndIndex + 1);
                        }

                        sb.DataSource = "tcp:" + originalServerNameWithInstance;
                        string tcpConnectionString = sb.ConnectionString;
                        using (SqlConnection con = new SqlConnection(tcpConnectionString))
                        {
                            con.Open();

                            SqlCommand cmd = con.CreateCommand();
                            cmd.CommandText = "select [local_tcp_port] from sys.dm_exec_connections where [session_id] = @@SPID";
                            serverPort = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        s_dataSourceToPortCache[cacheKey] = serverPort.Value;
                    }
                }
            }

            // override it with user-provided one
            string retDataSource;
            if (replaceServerName != null)
            {
                retDataSource = replaceServerName;
            }
            else
            {
                retDataSource = originalServerName;
            }

            // reconstruct the connection string (with the new server name and port)
            // also, no protocol is needed since TCP is enforced anyway if MultiSubnetFailover is set to true
            Debug.Assert(serverPort.HasValue, "Server port must be initialized");
            retDataSource += ", " + serverPort.Value;

            sb.DataSource = retDataSource;
            sb.MultiSubnetFailover = true;

            return sb;
        }
    }
}
