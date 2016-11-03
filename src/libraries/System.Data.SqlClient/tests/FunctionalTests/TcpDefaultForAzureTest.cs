using System.Diagnostics;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    [OuterLoop("Takes minutes on some networks")]
    public static class TcpDefaultForAzureTest
    {
        private const string NP = "Named Pipes Provider";
        private const string TCP = "TCP Provider";
        private const string InvalidHostname = "invalidHostname";
        private const string ErrorMessage = "A network-related or instance-specific error occurred while establishing a connection to SQL Server.";

        private static string[] AzureExtensions;
        private static SqlConnectionStringBuilder builder;

        static TcpDefaultForAzureTest()
        {
            AzureExtensions = new string[]
                {
                    ".database.windows.net",
                    ".database.cloudapi.de",
                    ".database.usgovcloudapi.net",
                    ".database.chinacloudapi.cn"
                };

            builder = new SqlConnectionStringBuilder();
            builder.ConnectTimeout = 1;
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void NonAzureNoProtocolConnectionTestOnWindows()
        {
            builder.DataSource = InvalidHostname;
            Assert.True(IsConnectionFailedOn(builder.ConnectionString, NP));
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void NonAzureNoProtocolConnectionTestOnUnix()
        {
            builder.DataSource = InvalidHostname;
            Assert.True(IsConnectionFailedOn(builder.ConnectionString, TCP));
        }


        [Fact]
        public static void NonAzureTcpConnectionTest()
        {
            builder.DataSource = "tcp:" + InvalidHostname;
            Assert.True(IsConnectionFailedOn(builder.ConnectionString, TCP));
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void NonAzureNpConnectionTest()
        {
            builder.DataSource = "np:\\\\" + InvalidHostname + "\\pipe\\sql\\query";
            Assert.True(IsConnectionFailedOn(builder.ConnectionString, NP));
        }


        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void AzureNoProtocolConnectionTest()
        {
            foreach (string extension in AzureExtensions)
            {
                builder.DataSource = InvalidHostname + extension;
                Assert.True(IsConnectionFailedOn(builder.ConnectionString, TCP));
            }
        }


        [Fact]
        public static void AzureTcpConnectionTest()
        {
            foreach (string extension in AzureExtensions)
            {
                builder.DataSource = "tcp:" + InvalidHostname + extension;
                Assert.True(IsConnectionFailedOn(builder.ConnectionString, TCP));
            }
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AzureNpConnectionTest()
        {
            foreach (string extension in AzureExtensions)
            {
                builder.DataSource = "np:\\\\" + InvalidHostname + extension + "\\pipe\\sql\\query";
                Assert.True(IsConnectionFailedOn(builder.ConnectionString, NP));
            }
        }


        private static bool IsConnectionFailedOn(string connString, string protocol)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connString));
            Debug.Assert(!string.IsNullOrWhiteSpace(protocol));
            Debug.Assert(protocol == NP || protocol == TCP);

            string errorMessage = Connect(connString);

            return errorMessage != null &&
                    errorMessage.Contains(ErrorMessage) &&
                    errorMessage.Contains(String.Format("provider: {0}, error", protocol));
        }


        private static string Connect(string connString)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(connString));

            string errorMessage = null;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                try
                {
                    connection.Open();
                    connection.Close();
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                }
            }

            return errorMessage;
        }
    }
}
