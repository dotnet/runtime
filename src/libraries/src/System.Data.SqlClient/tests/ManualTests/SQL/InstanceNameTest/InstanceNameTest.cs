// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class InstanceNameTest
    {
        [CheckConnStrSetupFact]
        public static void ConnectToSQLWithInstanceNameTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);

            bool proceed = true;
            string dataSourceStr = builder.DataSource.Replace("tcp:", "");
            string[] serverNamePartsByBackSlash = dataSourceStr.Split('\\');
            if (!dataSourceStr.Contains(",") && serverNamePartsByBackSlash.Length == 2)
            {
                string hostname = serverNamePartsByBackSlash[0];
                proceed = !String.IsNullOrWhiteSpace(hostname) && IsBrowserAlive(hostname);
            }

            if(proceed)
            {
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    connection.Close();
                }
            }
        }

        private static bool IsBrowserAlive(string browserHostname)
        {
            const int DefaultBrowserPort = 1434;
            const int sendTimeout = 1000;
            const int receiveTimeout = 1000;
            const byte ClntUcastEx = 0x03;

            byte[] requestPacket = new byte[] { ClntUcastEx };
            byte[] responsePacket = null;
            using (UdpClient client = new UdpClient(AddressFamily.InterNetwork))
            {
                try
                {
                    Task<int> sendTask = client.SendAsync(requestPacket, requestPacket.Length, browserHostname, DefaultBrowserPort);
                    Task<UdpReceiveResult> receiveTask = null;
                    if (sendTask.Wait(sendTimeout) && (receiveTask = client.ReceiveAsync()).Wait(receiveTimeout))
                    {
                        responsePacket = receiveTask.Result.Buffer;
                    }
                }
                catch { }
            }
            return responsePacket != null && responsePacket.Length > 0;
        }
    }
}
