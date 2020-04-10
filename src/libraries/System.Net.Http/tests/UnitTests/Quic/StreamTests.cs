using System.Net.Quic.Implementations.Managed;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class StreamTests
    {
        private readonly QuicClientConnectionOptions _clientOpts;
        private readonly QuicListenerOptions _serverOpts;
        private readonly ManagedQuicConnection _client;
        private readonly ManagedQuicConnection _server;

        private readonly TestHarness _harness;


        public StreamTests(ITestOutputHelper output)
        {
            _clientOpts = new QuicClientConnectionOptions();
            _serverOpts = new QuicListenerOptions
            {
                CertificateFilePath = TestHarness.CertificateFilePath,
                PrivateKeyFilePath = TestHarness.PrivateKeyFilePath
            };
            _client = new ManagedQuicConnection(_clientOpts);
            _server = new ManagedQuicConnection(_serverOpts);

            _harness = new TestHarness(output, _client);

            _harness.EstablishConnection(_client, _server);
        }

        [Fact]
        public void SendSimpleUnidirectionalStream()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);

            _harness.SendFlight(_client, _server);

            var serverStream = _server.AcceptStream();
            Assert.NotNull(serverStream);

            var read = new byte[data.Length];
            serverStream.InboundBuffer!.Deliver(read);

            Assert.Equal(data, read);
        }
    }
}
