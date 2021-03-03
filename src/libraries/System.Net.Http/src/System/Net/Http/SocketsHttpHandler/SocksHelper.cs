// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static class SocksHelper
    {
        // socks protocol limits address length to 1 byte, thus the maximum possible buffer is 256+other fields
        private const int BufferSize = 512;
        private const int ProtocolVersion4 = 4;
        private const int ProtocolVersion5 = 5;
        private const byte METHOD_NO_AUTH = 0;
        // private const byte METHOD_GSSAPI = 1;
        private const byte METHOD_USERNAME_PASSWORD = 2;
        private const byte METHOD_NO_ACCEPTABLE = 0xFF;
        private const byte CMD_CONNECT = 1;
        // private const byte CMD_BIND = 2;
        // private const byte CMD_UDP_ASSOCIATE = 3;
        private const byte ATYP_IPV4 = 1;
        private const byte ATYP_DOMAIN_NAME = 3;
        private const byte ATYP_IPV6 = 4;
        private const byte REP_SUCCESS = 0;
        // private const byte REP_FAILURE = 1;
        // private const byte REP_NOT_ALLOWED = 2;
        // private const byte REP_NETWORK_UNREACHABLE = 3;
        // private const byte REP_HOST_UNREACHABLE = 4;
        // private const byte REP_CONNECTION_REFUSED = 5;
        // private const byte REP_TTL_EXPIRED = 6;
        // private const byte REP_CMD_NOT_SUPPORT = 7;
        // private const byte REP_ATYP_NOT_SUPPORT = 8;
        private const byte CD_SUCCESS = 90;

        public static async ValueTask EstablishSocksTunnelAsync(Stream stream, string host, int port, Uri proxyUri, ICredentials? proxyCredentials, bool async, CancellationToken cancellationToken)
        {
            // in sync path, dispose the stream to cancel
            using (cancellationToken.Register(() => stream.Dispose()))
            {
                try
                {
                    if (string.Equals(proxyUri.Scheme, "socks5", StringComparison.OrdinalIgnoreCase))
                    {
                        await EstablishSocks5TunnelAsync(stream, host, port, proxyUri, proxyCredentials, async, cancellationToken).ConfigureAwait(false);
                    }
                    else if (string.Equals(proxyUri.Scheme, "socks4a", StringComparison.OrdinalIgnoreCase))
                    {
                        await EstablishSocks4TunnelAsync(stream, true, host, port, proxyUri, proxyCredentials, async, cancellationToken).ConfigureAwait(false);
                    }
                    else if (string.Equals(proxyUri.Scheme, "socks4", StringComparison.OrdinalIgnoreCase))
                    {
                        await EstablishSocks4TunnelAsync(stream, false, host, port, proxyUri, proxyCredentials, async, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        Debug.Fail("Bad socks version.");
                    }
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
            }
        }

        private static async ValueTask EstablishSocks5TunnelAsync(Stream stream, string host, int port, Uri proxyUri, ICredentials? proxyCredentials, bool async, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                // https://tools.ietf.org/html/rfc1928

                // +----+----------+----------+
                // |VER | NMETHODS | METHODS  |
                // +----+----------+----------+
                // | 1  |    1     | 1 to 255 |
                // +----+----------+----------+
                buffer[0] = ProtocolVersion5;
                var credentials = proxyCredentials?.GetCredential(proxyUri, "");
                if (credentials != null)
                {
                    buffer[1] = 1;
                    buffer[2] = METHOD_NO_AUTH;
                }
                else
                {
                    buffer[1] = 2;
                    buffer[2] = METHOD_NO_AUTH;
                    buffer[3] = METHOD_USERNAME_PASSWORD;
                }
                await WriteAsync(stream, buffer.AsMemory(0, buffer[1] + 2), async, cancellationToken).ConfigureAwait(false);

                // +----+--------+
                // |VER | METHOD |
                // +----+--------+
                // | 1  |   1    |
                // +----+--------+
                await ReadToFillAsync(stream, buffer.AsMemory(0, 2), async, cancellationToken).ConfigureAwait(false);
                if (buffer[0] != ProtocolVersion5)
                    throw new Exception("Bad protocol version");

                switch (buffer[1])
                {
                    case METHOD_NO_AUTH:
                        // continue
                        break;

                    case METHOD_USERNAME_PASSWORD:
                        {
                            // https://tools.ietf.org/html/rfc1929
                            if (credentials == null)
                                throw new Exception("Server choses bad auth method.");

                            // +----+------+----------+------+----------+
                            // |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
                            // +----+------+----------+------+----------+
                            // | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
                            // +----+------+----------+------+----------+
                            buffer[0] = ProtocolVersion5;
                            int uLen = Encoding.UTF8.GetByteCount(credentials.UserName);
                            buffer[1] = checked((byte)uLen);
                            int uLenEncoded = Encoding.UTF8.GetBytes(credentials.UserName, buffer.AsSpan(2));
                            Debug.Assert(uLen == uLenEncoded);
                            int pLen = Encoding.UTF8.GetByteCount(credentials.Password);
                            buffer[2 + uLen] = checked((byte)pLen);
                            int pLenEncoded = Encoding.UTF8.GetBytes(credentials.Password, buffer.AsSpan(3 + uLen));
                            Debug.Assert(pLen == pLenEncoded);
                            await WriteAsync(stream, buffer.AsMemory(0, 4 + uLen + pLen), async, cancellationToken).ConfigureAwait(false);

                            // +----+--------+
                            // |VER | STATUS |
                            // +----+--------+
                            // | 1  |   1    |
                            // +----+--------+
                            await ReadToFillAsync(stream, buffer.AsMemory(0, 2), async, cancellationToken).ConfigureAwait(false);
                            if (buffer[0] != ProtocolVersion5)
                                throw new Exception("Bad protocol version");
                            if (buffer[1] != REP_SUCCESS)
                                throw new Exception("Authentication failed.");
                            break;
                        }

                    case METHOD_NO_ACCEPTABLE:
                    default:
                        throw new Exception("No acceptable auth method.");
                }


                // +----+-----+-------+------+----------+----------+
                // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                buffer[0] = ProtocolVersion5;
                buffer[1] = CMD_CONNECT;
                buffer[2] = 0;
                int addressLength;

                if (IPAddress.TryParse(host, out var hostIP))
                {
                    if (hostIP.AddressFamily == Sockets.AddressFamily.InterNetwork)
                    {
                        buffer[3] = ATYP_IPV4;
                        hostIP.TryWriteBytes(buffer.AsSpan(4), out int bytesWritten);
                        Debug.Assert(bytesWritten == 4);
                        addressLength = 3;
                    }
                    else
                    {
                        Debug.Assert(hostIP.AddressFamily == Sockets.AddressFamily.InterNetworkV6);
                        buffer[3] = ATYP_IPV6;
                        hostIP.TryWriteBytes(buffer.AsSpan(4), out int bytesWritten);
                        Debug.Assert(bytesWritten == 16);
                        addressLength = 15;
                    }
                }
                else
                {
                    buffer[3] = ATYP_DOMAIN_NAME;
                    addressLength = Encoding.UTF8.GetByteCount(host);
                    buffer[4] = checked((byte)addressLength);
                    int bytesEncoded = Encoding.UTF8.GetBytes(host, buffer.AsSpan(5));
                    Debug.Assert(bytesEncoded == addressLength);
                }

                Debug.Assert(port > 0);
                Debug.Assert(port < ushort.MaxValue);
                buffer[addressLength + 5] = (byte)(port >> 8);
                buffer[addressLength + 6] = (byte)port;

                await WriteAsync(stream, buffer.AsMemory(0, addressLength + 7), async, cancellationToken).ConfigureAwait(false);

                // +----+-----+-------+------+----------+----------+
                // |VER | REP |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                await ReadToFillAsync(stream, buffer.AsMemory(0, 5), async, cancellationToken).ConfigureAwait(false);
                if (buffer[0] != ProtocolVersion5)
                    throw new Exception("Bad protocol version");
                if (buffer[1] != REP_SUCCESS)
                    throw new Exception("Connection failed");
                int bytesToSkip = buffer[3] switch
                {
                    ATYP_IPV4 => 5,
                    ATYP_IPV6 => 17,
                    ATYP_DOMAIN_NAME => buffer[4] + 2,
                    _ => throw new Exception("Unknown address type")
                };
                await ReadToFillAsync(stream, buffer.AsMemory(0, bytesToSkip), async, cancellationToken).ConfigureAwait(false);
                // response address not used
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async ValueTask EstablishSocks4TunnelAsync(Stream stream, bool isVersion4a, string host, int port, Uri proxyUri, ICredentials? proxyCredentials, bool async, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                // https://www.openssh.com/txt/socks4.protocol

                // +----+----+----+----+----+----+----+----+----+----+....+----+
                // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
                // +----+----+----+----+----+----+----+----+----+----+....+----+
                //    1    1      2              4           variable       1
                string? username = proxyCredentials?.GetCredential(proxyUri, "")?.UserName;
                buffer[0] = ProtocolVersion4;
                buffer[1] = CMD_CONNECT;

                Debug.Assert(port > 0);
                Debug.Assert(port < ushort.MaxValue);
                buffer[2] = (byte)(port >> 8);
                buffer[3] = (byte)port;

                IPAddress? ipv4Address = null;
                if (IPAddress.TryParse(host, out var hostIP))
                {
                    if (hostIP.AddressFamily == Sockets.AddressFamily.InterNetwork)
                    {
                        ipv4Address = hostIP;
                    }
                    else if (hostIP.IsIPv4MappedToIPv6)
                    {
                        ipv4Address = hostIP.MapToIPv4();
                    }
                    else
                    {
                        throw new Exception("SOCKS4 does not support IPv6.");
                    }
                }
                else if (!isVersion4a)
                {
                    // SOCKS4 requires DNS resolution locally
                    var addresses = async
                        ? await Dns.GetHostAddressesAsync(host, Sockets.AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false)
                        : Dns.GetHostAddresses(host);

                    if (addresses.Length == 0)
                    {
                        throw new Exception("No suitable IPv4 address.");
                    }

                    ipv4Address = addresses[0];
                }

                if (ipv4Address == null)
                {
                    buffer[4] = 0;
                    buffer[5] = 0;
                    buffer[6] = 0;
                    buffer[7] = 255;
                }
                else
                {
                    ipv4Address.TryWriteBytes(buffer.AsSpan(4), out int bytesWritten);
                    Debug.Assert(bytesWritten == 4);
                    if (buffer[4] == 0 && buffer[5] == 0 && buffer[6] == 0)
                    {
                        // Invalid IP address used by SOCKS4a to represent remote DNS.
                        // In case we don't have a domain name, throwing.
                        throw new Exception("Invalid ip address.");
                    }
                }

                int uLen = Encoding.UTF8.GetBytes(username, buffer.AsSpan(8));
                buffer[8 + uLen] = 0;
                int totalLength = 9 + uLen;

                if (isVersion4a && ipv4Address == null)
                {
                    // https://www.openssh.com/txt/socks4a.protocol
                    int aLen = Encoding.UTF8.GetBytes(host, buffer.AsSpan(9 + uLen));
                    buffer[9 + uLen + aLen] = 0;
                    totalLength += aLen + 1;
                }

                await WriteAsync(stream, buffer.AsMemory(0, totalLength), async, cancellationToken).ConfigureAwait(false);

                // +----+----+----+----+----+----+----+----+
                // | VN | CD | DSTPORT |      DSTIP        |
                // +----+----+----+----+----+----+----+----+
                //    1    1      2              4
                await ReadToFillAsync(stream, buffer.AsMemory(0, 8), async, cancellationToken).ConfigureAwait(false);
                if (buffer[0] != ProtocolVersion4)
                {
                    throw new Exception("Bad protocol version");
                }
                if (buffer[1] != CD_SUCCESS)
                {
                    throw new Exception("Connection failed");
                }
                // response address not used
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async ValueTask WriteAsync(Stream stream, Memory<byte> buffer, bool async, CancellationToken cancellationToken)
        {
            if (async)
            {
                await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                stream.Write(buffer.Span);
            }
        }

        private static async ValueTask ReadToFillAsync(Stream stream, Memory<byte> buffer, bool async, CancellationToken cancellationToken)
        {
            while (!buffer.IsEmpty)
            {
                int bytesRead = async
                    ? await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)
                    : stream.Read(buffer.Span);

                if (bytesRead == 0)
                {
                    throw new Exception("Early EOF");
                }

                buffer = buffer[bytesRead..];
            }
        }
    }
}
