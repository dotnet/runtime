// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
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
            using (cancellationToken.Register(s => ((Stream)s!).Dispose(), stream))
            {
                try
                {
                    if (string.Equals(proxyUri.Scheme, "socks5", StringComparison.OrdinalIgnoreCase))
                    {
                        await EstablishSocks5TunnelAsync(stream, host, port, proxyUri, proxyCredentials, async).ConfigureAwait(false);
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

        private static async ValueTask EstablishSocks5TunnelAsync(Stream stream, string host, int port, Uri proxyUri, ICredentials? proxyCredentials, bool async)
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
                await WriteAsync(stream, buffer.AsMemory(0, buffer[1] + 2), async).ConfigureAwait(false);

                // +----+--------+
                // |VER | METHOD |
                // +----+--------+
                // | 1  |   1    |
                // +----+--------+
                await ReadToFillAsync(stream, buffer.AsMemory(0, 2), async).ConfigureAwait(false);
                VerifyProtocolVersion(ProtocolVersion5, buffer[0]);

                switch (buffer[1])
                {
                    case METHOD_NO_AUTH:
                        // continue
                        break;

                    case METHOD_USERNAME_PASSWORD:
                        {
                            // https://tools.ietf.org/html/rfc1929
                            if (credentials == null)
                            {
                                throw new SocksException(SR.net_socks_no_auth_method);
                            }

                            // +----+------+----------+------+----------+
                            // |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
                            // +----+------+----------+------+----------+
                            // | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
                            // +----+------+----------+------+----------+
                            buffer[0] = ProtocolVersion5;
                            int usernameLength = Encoding.UTF8.GetByteCount(credentials.UserName);
                            buffer[1] = checked((byte)usernameLength);
                            int usernameLengthEncoded = Encoding.UTF8.GetBytes(credentials.UserName, buffer.AsSpan(2));
                            Debug.Assert(usernameLength == usernameLengthEncoded);
                            int passwordLength = Encoding.UTF8.GetByteCount(credentials.Password);
                            buffer[2 + usernameLength] = checked((byte)passwordLength);
                            int passwordLengthEncoded = Encoding.UTF8.GetBytes(credentials.Password, buffer.AsSpan(3 + usernameLength));
                            Debug.Assert(passwordLength == passwordLengthEncoded);
                            await WriteAsync(stream, buffer.AsMemory(0, 4 + usernameLength + passwordLength), async).ConfigureAwait(false);

                            // +----+--------+
                            // |VER | STATUS |
                            // +----+--------+
                            // | 1  |   1    |
                            // +----+--------+
                            await ReadToFillAsync(stream, buffer.AsMemory(0, 2), async).ConfigureAwait(false);
                            VerifyProtocolVersion(ProtocolVersion5, buffer[0]);
                            if (buffer[1] != REP_SUCCESS)
                            {
                                throw new SocksException(SR.net_socks_auth_failed);
                            }
                            break;
                        }

                    case METHOD_NO_ACCEPTABLE:
                    default:
                        throw new SocksException(SR.net_socks_no_auth_method);
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
                    if (hostIP.AddressFamily == AddressFamily.InterNetwork)
                    {
                        buffer[3] = ATYP_IPV4;
                        hostIP.TryWriteBytes(buffer.AsSpan(4), out int bytesWritten);
                        Debug.Assert(bytesWritten == 4);
                        addressLength = 4;
                    }
                    else
                    {
                        Debug.Assert(hostIP.AddressFamily == AddressFamily.InterNetworkV6);
                        buffer[3] = ATYP_IPV6;
                        hostIP.TryWriteBytes(buffer.AsSpan(4), out int bytesWritten);
                        Debug.Assert(bytesWritten == 16);
                        addressLength = 16;
                    }
                }
                else
                {
                    buffer[3] = ATYP_DOMAIN_NAME;
                    int hostLength = Encoding.UTF8.GetByteCount(host);
                    buffer[4] = checked((byte)hostLength);
                    int bytesEncoded = Encoding.UTF8.GetBytes(host, buffer.AsSpan(5));
                    Debug.Assert(bytesEncoded == hostLength);
                    addressLength = hostLength + 1;
                }

                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(addressLength + 4), (ushort)port);

                await WriteAsync(stream, buffer.AsMemory(0, addressLength + 6), async).ConfigureAwait(false);

                // +----+-----+-------+------+----------+----------+
                // |VER | REP |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                await ReadToFillAsync(stream, buffer.AsMemory(0, 5), async).ConfigureAwait(false);
                VerifyProtocolVersion(ProtocolVersion5, buffer[0]);
                if (buffer[1] != REP_SUCCESS)
                {
                    throw new SocksException(SR.net_socks_connection_failed);
                }
                int bytesToSkip = buffer[3] switch
                {
                    ATYP_IPV4 => 5,
                    ATYP_IPV6 => 17,
                    ATYP_DOMAIN_NAME => buffer[4] + 2,
                    _ => throw new SocksException(SR.net_socks_bad_address_type)
                };
                await ReadToFillAsync(stream, buffer.AsMemory(0, bytesToSkip), async).ConfigureAwait(false);
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

                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), (ushort)port);

                IPAddress? ipv4Address = null;
                if (IPAddress.TryParse(host, out var hostIP))
                {
                    if (hostIP.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipv4Address = hostIP;
                    }
                    else if (hostIP.IsIPv4MappedToIPv6)
                    {
                        ipv4Address = hostIP.MapToIPv4();
                    }
                    else
                    {
                        throw new SocksException(SR.net_socks_ipv6_notsupported);
                    }
                }
                else if (!isVersion4a)
                {
                    // SOCKS4 requires DNS resolution locally
                    var addresses = async
                        ? await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false)
                        : Dns.GetHostAddresses(host, AddressFamily.InterNetwork);

                    if (addresses.Length == 0)
                    {
                        throw new SocksException(SR.net_socks_no_ipv4_address);
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
                        throw new SocksException(SR.net_socks_ipv4_invalid);
                    }
                }

                int usernameLength = Encoding.UTF8.GetBytes(username, buffer.AsSpan(8));
                buffer[8 + usernameLength] = 0;
                int totalLength = 9 + usernameLength;

                if (isVersion4a && ipv4Address == null)
                {
                    // https://www.openssh.com/txt/socks4a.protocol
                    int hostLength = Encoding.UTF8.GetBytes(host, buffer.AsSpan(9 + usernameLength));
                    buffer[totalLength + hostLength] = 0;
                    totalLength += hostLength + 1;
                }

                await WriteAsync(stream, buffer.AsMemory(0, totalLength), async).ConfigureAwait(false);

                // +----+----+----+----+----+----+----+----+
                // | VN | CD | DSTPORT |      DSTIP        |
                // +----+----+----+----+----+----+----+----+
                //    1    1      2              4
                await ReadToFillAsync(stream, buffer.AsMemory(0, 8), async).ConfigureAwait(false);
                VerifyProtocolVersion(ProtocolVersion4, buffer[0]);
                if (buffer[1] != CD_SUCCESS)
                {
                    throw new SocksException(SR.net_socks_connection_failed);
                }
                // response address not used
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void VerifyProtocolVersion(byte expected, byte version)
        {
            if (expected != version)
            {
                throw new SocksException(SR.Format(SR.net_socks_unexpected_version, expected, version));
            }
        }

        private static ValueTask WriteAsync(Stream stream, Memory<byte> buffer, bool async)
        {
            if (async)
            {
                return stream.WriteAsync(buffer);
            }
            else
            {
                stream.Write(buffer.Span);
                return default;
            }
        }

        private static async ValueTask ReadToFillAsync(Stream stream, Memory<byte> buffer, bool async)
        {
            while (!buffer.IsEmpty)
            {
                int bytesRead = async
                    ? await stream.ReadAsync(buffer).ConfigureAwait(false)
                    : stream.Read(buffer.Span);

                if (bytesRead == 0)
                {
                    throw new IOException(SR.net_http_invalid_response_premature_eof);
                }

                buffer = buffer[bytesRead..];
            }
        }
    }
}
