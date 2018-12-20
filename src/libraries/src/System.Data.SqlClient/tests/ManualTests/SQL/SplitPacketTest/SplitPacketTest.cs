// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SplitPacketTest
    {
        private int Port = -1;
        private int SplitPacketSize = 1;
        private string BaseConnString;

        public SplitPacketTest()
        {
            string actualHost;
            int actualPort;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            GetTcpInfoFromDataSource(builder.DataSource, out actualHost, out actualPort);

            Task.Factory.StartNew(() => { SetupProxy(actualHost, actualPort); });

            for(int i = 0; i < 10 && Port == -1; i++)
            {
                Thread.Sleep(500);
            }
            if (Port == -1) throw new InvalidOperationException("Proxy local port not defined!");

            builder.DataSource = "tcp:127.0.0.1," + Port;
            BaseConnString = builder.ConnectionString;
        } 

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void OneByteSplitTest()
        {
            SplitPacketSize = 1;
            OpenConnection();
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void AlmostFullHeaderTest()
        {
            SplitPacketSize = 7;
            OpenConnection();
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void FullHeaderTest()
        {
            SplitPacketSize = 8;
            OpenConnection();
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void HeaderPlusOneTest()
        {
            SplitPacketSize = 9;
            OpenConnection();
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void MARSSplitTest()
        {
            SplitPacketSize = 1;
            OpenMarsConnection("select * from Orders");
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void MARSReplicateTest()
        {
            SplitPacketSize = 1;
            OpenMarsConnection("select REPLICATE('A', 10000)");
        }

        private void OpenMarsConnection(string cmdText)
        {
            using (SqlConnection conn = new SqlConnection((new SqlConnectionStringBuilder(BaseConnString) { MultipleActiveResultSets = true }).ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd1 = new SqlCommand(cmdText, conn))
                using (SqlCommand cmd2 = new SqlCommand(cmdText, conn))
                using (SqlCommand cmd3 = new SqlCommand(cmdText, conn))
                using (SqlCommand cmd4 = new SqlCommand(cmdText, conn))
                {
                    cmd1.ExecuteReader();
                    cmd2.ExecuteReader();
                    cmd3.ExecuteReader();
                    cmd4.ExecuteReader();
                }
                conn.Close();
            }
        }

        private void OpenConnection()
        {
            using (SqlConnection conn = new SqlConnection(BaseConnString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select * from Orders", conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    Assert.True(reader.HasRows, "Split packet query did not return any rows!");
                }
                conn.Close();
            }
        }

        private void SetupProxy(string actualHost, int actualPort)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var client = listener.AcceptTcpClientAsync().GetAwaiter().GetResult();

            var sqlClient = new TcpClient();
            sqlClient.ConnectAsync(actualHost, actualPort).Wait();

            Task.Factory.StartNew(() => { ForwardToSql(client, sqlClient); });
            Task.Factory.StartNew(() => { ForwardToClient(client, sqlClient); });
        }

        private void ForwardToSql(TcpClient ourClient, TcpClient sqlClient)
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = ourClient.GetStream().Read(buffer, 0, buffer.Length);

                sqlClient.GetStream().Write(buffer, 0, bytesRead);
            }
        }

        private void ForwardToClient(TcpClient ourClient, TcpClient sqlClient)
        {
            while (true)
            {
                byte[] buffer = new byte[SplitPacketSize];
                int bytesRead = sqlClient.GetStream().Read(buffer, 0, buffer.Length);

                ourClient.GetStream().Write(buffer, 0, bytesRead);

                buffer = new byte[1024];
                bytesRead = sqlClient.GetStream().Read(buffer, 0, buffer.Length);

                ourClient.GetStream().Write(buffer, 0, bytesRead);
            }
        }

        private static void GetTcpInfoFromDataSource(string dataSource, out string hostName, out int port)
        {
            string[] dataSourceParts = dataSource.Split(',');
            if(dataSourceParts.Length == 1)
            {
                hostName = dataSourceParts[0].Replace("tcp:", "");
                port = 1433;
            }
            else if(dataSourceParts.Length == 2)
            {
                hostName = dataSourceParts[0].Replace("tcp:", "");
                port = int.Parse(dataSourceParts[1]);
            }
            else
            {
                throw new InvalidOperationException("TCP Connection String not in correct format!");
            }
        }
    }
}