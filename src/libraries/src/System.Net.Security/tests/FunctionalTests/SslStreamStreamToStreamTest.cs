// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    public class SslStreamStreamToStreamTest
    {
        private readonly byte[] sampleMsg = Encoding.UTF8.GetBytes("Sample Test Message");
        private readonly TimeSpan TaskTimeSpan = TimeSpan.FromSeconds(15);

        #region Tests

        [Fact]
        public void SslStream_StreamToStream_Authentication_Success()
        {
            MockNetwork network = new MockNetwork();

            using (var clientStream = new FakeNetworkStream(false, network))
            using (var serverStream = new FakeNetworkStream(true, network))
            using (var client = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            {
                X509Certificate2 certificate = TestConfiguration.GetServerCertificate();
                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync(certificate.Subject);
                auth[1] = server.AuthenticateAsServerAsync(certificate);

                bool finished = Task.WaitAll(auth, TimeSpan.FromSeconds(3));
                Assert.True(finished, "Handshake completed in the allotted time");
            }
        }

        [Fact]
        public void SslStream_StreamToStream_Authentication_IncorrectServerName_Fail()
        {
            MockNetwork network = new MockNetwork();

            using (var clientStream = new FakeNetworkStream(false, network))
            using (var serverStream = new FakeNetworkStream(true, network))
            using (var client = new SslStream(clientStream))
            using (var server = new SslStream(serverStream))
            {
                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync("incorrectServer");
                auth[1] = server.AuthenticateAsServerAsync(TestConfiguration.GetServerCertificate());

                Assert.Throws<AuthenticationException>(() =>
                {
                    auth[0].GetAwaiter().GetResult();
                });

                auth[1].GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Tests that a second clientSslStream.write after first clientSslStream.write is successfull.
        /// </summary>
        [Fact]
        public void SslStream_StreamToStream_Successive_ClientWrite_Success()
        {
            byte[] recvBuf = new byte[sampleMsg.Length];
            MockNetwork network = new MockNetwork();

            using (var clientStream = new FakeNetworkStream(false, network))
            using (var serverStream = new FakeNetworkStream(true, network))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                bool result = DoHandshake(clientSslStream, serverSslStream, TaskTimeSpan);

                Assert.True(result, "Handshake completed.");

                clientSslStream.Write(sampleMsg);

                serverSslStream.Read(recvBuf, 0, sampleMsg.Length);

                clientSslStream.Write(sampleMsg);

                // TODO Issue#3802
                // The condition on which read method (UpdateReadStream) in FakeNetworkStream does a network read is flawed.
                // That works fine in single read/write but fails in multi read write as stream size can be more, but real data can be < stream size.
                // So I am doing an explicit read here. This issue is specific to test only & irrespective of xplat.
                serverStream.DoNetworkRead();

                serverSslStream.Read(recvBuf, 0, sampleMsg.Length);

                Assert.True(VerifyOutput(recvBuf, sampleMsg), "verify second read data is as expected.");
            }
        }

        #endregion

        #region Private Methods

        private bool VerifyOutput(byte [] actualBuffer, byte [] expectedBuffer)
        {
            return expectedBuffer.SequenceEqual(actualBuffer);
        }

        private bool AllowAnyServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }

            return false;
        }

        private bool DoHandshake(SslStream clientSslStream, SslStream serverSslStream, TimeSpan waitTimeSpan)
        {
            X509Certificate2 certificate = TestConfiguration.GetServerCertificate();
            Task[] auth = new Task[2];

            auth[0] = clientSslStream.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));
            auth[1] = serverSslStream.AuthenticateAsServerAsync(certificate);

            bool finished = Task.WaitAll(auth, waitTimeSpan);
            return finished;
        }

        #endregion
    }
}
