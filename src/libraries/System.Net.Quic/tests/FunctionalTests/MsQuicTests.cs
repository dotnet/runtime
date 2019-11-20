using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class MsQuicTests
    {
        private static ReadOnlyMemory<byte> s_data = Encoding.UTF8.GetBytes("Hello world!");

        [Fact]
        public async Task BasicTest()
        {
            // TODO to compare:
            // Verify same cert as kestrel
            // Get stub tls
            // Figure out the
            //            Exception thrown at 0x00007FF920D25A29 in dotnet.exe: Microsoft C++exception: EETypeLoadException at memory location 0x0000003620BEE990.
            //Exception thrown at 0x00007FF920D25A29 in dotnet.exe: Microsoft C++exception: [rethrow] at memory location 0x0000000000000000.
            //Exception thrown at 0x00007FF920D25A29 in dotnet.exe: Microsoft C++ exception: EETypeLoadException at memory location 0x0000003620BEE990.
            var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", false);

            var sslServerOptions = new SslServerAuthenticationOptions()
            {
                ServerCertificate = cers[0], // TODO add cert
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
            };

            var sslClientOptions = new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
            };

            store.Close();

            // Race where client starts connection first?

            using (QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, new IPEndPoint(IPAddress.Loopback, 8000), sslServerOptions))
            {
                IPEndPoint listenEndPoint = listener.ListenEndPoint;

                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        for (var i = 0; i < 100; i++)
                        {
                            try
                            {
                                // TODO check what happens without task.delay
                                // TODO remove read here and make sure nothing aborts
                                // TODO graceful shutdown.
                                // Client code
                                await Task.Delay(100);
                                using (QuicConnection connection = new QuicConnection(QuicImplementationProviders.MsQuic, listenEndPoint, sslClientOptions))
                                {
                                    await connection.ConnectAsync();
                                    for (var j = 0; j < 100; j++)
                                    {
                                        using (QuicStream stream = connection.OpenBidirectionalStream())
                                        {
                                            await stream.WriteAsync(s_data);
                                            var memory = new byte[12];
                                            var res = await stream.ReadAsync(memory);
                                            Assert.True(s_data.Span.SequenceEqual(memory));
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }),
                    Task.Run(async () =>
                    {
                        // Server code
                        for (var i = 0; i < 100; i++)
                        {
                            try
                            {
                                using (QuicConnection connection = await listener.AcceptConnectionAsync())
                                {
                                    for (var j = 0; j < 100; j++)
                                    {
                                        using (QuicStream stream = await connection.AcceptStreamAsync())
                                        {
                                            byte[] buffer = new byte[s_data.Length];
                                            int bytesRead = await stream.ReadAsync(buffer);
                                            Assert.Equal(s_data.Length, bytesRead);
                                            Assert.True(s_data.Span.SequenceEqual(buffer));
                                            await stream.WriteAsync(s_data);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }));
            }
        }
    }
}
