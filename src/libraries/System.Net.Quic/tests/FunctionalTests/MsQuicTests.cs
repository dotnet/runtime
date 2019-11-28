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

        //[Fact]
        //public async Task BasicTest()
        //{
        //    // TODO to compare:
        //    // Get stub tls
        //    var store = new X509Store(StoreLocation.LocalMachine);
        //    store.Open(OpenFlags.ReadOnly);
        //    X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", false);

        //    var sslServerOptions = new SslServerAuthenticationOptions()
        //    {
        //        ServerCertificate = cers[0],
        //        ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
        //    };

        //    var sslClientOptions = new SslClientAuthenticationOptions()
        //    {
        //        ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("quictest") }
        //    };

        //    store.Close();

        //    // Race where client starts connection first?

        //    using (QuicListener listener = new QuicListener(QuicImplementationProviders.MsQuic, new IPEndPoint(IPAddress.Loopback, 8000), sslServerOptions))
        //    {
        //        IPEndPoint listenEndPoint = listener.ListenEndPoint;

        //        await Task.WhenAll(
        //            Task.Run(async () =>
        //            {
        //                for (var i = 0; i < 3; i++)
        //                {
        //                    // TODO need to make sure listener is up before client.
        //                    await Task.Delay(100);
        //                    using (QuicConnection connection = new QuicConnection(QuicImplementationProviders.MsQuic, listenEndPoint, sslClientOptions))
        //                    {
        //                        await connection.ConnectAsync();
        //                        for (int j = 0; j < 3; j++)
        //                        {
        //                            using (QuicStream stream = connection.OpenBidirectionalStream())
        //                            {
        //                                await stream.WriteAsync(s_data);
        //                                byte[] memory = new byte[12];
        //                                int res = await stream.ReadAsync(memory);
        //                                Assert.True(s_data.Span.SequenceEqual(memory));
        //                            }
        //                        }
        //                    }
        //                }
        //            }),
        //            Task.Run(async () =>
        //            {
        //                // Server code
        //                for (var i = 0; i < 3; i++)
        //                {
        //                    try
        //                    {
        //                        using (QuicConnection connection = await listener.AcceptConnectionAsync())
        //                        {
        //                            for (var j = 0; j < 3; j++)
        //                            {

        //                                using (QuicStream stream = await connection.AcceptStreamAsync())
        //                                {
        //                                    try
        //                                    {
        //                                        byte[] buffer = new byte[s_data.Length];
        //                                        int bytesRead = await stream.ReadAsync(buffer);
        //                                        Assert.Equal(s_data.Length, bytesRead);
        //                                        Assert.True(s_data.Span.SequenceEqual(buffer));
        //                                        await stream.WriteAsync(s_data);
        //                                    }
        //                                    catch (Exception ex)
        //                                    {
        //                                        Console.WriteLine(ex.Message);
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Console.WriteLine(ex.Message);
        //                    }
        //                }
        //            }));
        //    }
        //}


        [Fact]
        public async Task MultipleReadsAndWrites()
        {
            // TODO to compare:
            // Get stub tls
            var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", false);

            var sslServerOptions = new SslServerAuthenticationOptions()
            {
                ServerCertificate = cers[0],
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
                        // TODO need to make sure listener is up before client.
                        await Task.Delay(100);
                        using (QuicConnection connection = new QuicConnection(QuicImplementationProviders.MsQuic, listenEndPoint, sslClientOptions))
                        {
                            await connection.ConnectAsync();
                            using (QuicStream stream = connection.OpenBidirectionalStream())
                            {
                                for (var i = 0; i < 100; i++)
                                {
                                    await stream.WriteAsync(s_data);
                                }

                                stream.ShutdownWrite();

                                byte[] memory = new byte[12];
                                while (true)
                                {
                                    var res = await stream.ReadAsync(memory);
                                    if (res == 0)
                                    {
                                        break;
                                    }
                                    Assert.True(s_data.Span.SequenceEqual(memory));
                                }
                            }
                        }
                    }),
                    Task.Run(async () =>
                    {
                        // Server code
                        using (QuicConnection connection = await listener.AcceptConnectionAsync())
                        {

                            using (QuicStream stream = await connection.AcceptStreamAsync())
                            {
                                byte[] buffer = new byte[s_data.Length];
                                while (true)
                                {
                                    int bytesRead = await stream.ReadAsync(buffer);
                                    if (bytesRead == 0)
                                    {
                                        break;
                                    }
                                    Assert.Equal(s_data.Length, bytesRead);
                                    Assert.True(s_data.Span.SequenceEqual(buffer));
                                }
                                for (int i = 0; i < 100; i++)
                                {
                                    await stream.WriteAsync(s_data);
                                }
                            }
                        }
                    }));
            }
        }
    }
}
