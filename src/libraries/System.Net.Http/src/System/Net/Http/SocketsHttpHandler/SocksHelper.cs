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

        public static async ValueTask EstablishSocks5TunnelAsync(Stream stream, string host, int port, Uri proxyUri, ICredentials? proxyCredentials, CancellationToken cancellationToken)
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
                await stream.WriteAsync(buffer.AsMemory(0, buffer[1] + 2), cancellationToken).ConfigureAwait(false);

                // +----+--------+
                // |VER | METHOD |
                // +----+--------+
                // | 1  |   1    |
                // +----+--------+
                await stream.ReadAsync(buffer.AsMemory(0, 2), cancellationToken).ConfigureAwait(false);
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
                            await stream.WriteAsync(buffer.AsMemory(0, 4 + uLen + pLen), cancellationToken).ConfigureAwait(false);

                            // +----+--------+
                            // |VER | STATUS |
                            // +----+--------+
                            // | 1  |   1    |
                            // +----+--------+
                            await stream.ReadAsync(buffer.AsMemory(0, 2), cancellationToken).ConfigureAwait(false);
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
                buffer[3] = ATYP_DOMAIN_NAME;

                int addressLength = Encoding.UTF8.GetByteCount(host);
                buffer[4] = checked((byte)addressLength);
                int bytesEncoded = Encoding.UTF8.GetBytes(host, buffer.AsSpan(5));
                Debug.Assert(bytesEncoded == addressLength);

                Debug.Assert(port > 0);
                Debug.Assert(port < ushort.MaxValue);
                buffer[addressLength + 5] = (byte)(port >> 8);
                buffer[addressLength + 6] = (byte)port;

                await stream.WriteAsync(buffer.AsMemory(0, addressLength + 7), cancellationToken).ConfigureAwait(false);

                // +----+-----+-------+------+----------+----------+
                // |VER | REP |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                await stream.ReadAsync(buffer.AsMemory(0, 4), cancellationToken).ConfigureAwait(false);
                if (buffer[0] != ProtocolVersion5)
                    throw new Exception("Bad protocol version");
                if (buffer[1] != REP_SUCCESS)
                    throw new Exception("Connection failed");
                switch (buffer[3])
                {
                    case ATYP_IPV4:
                        await stream.ReadAsync(buffer.AsMemory(0, 4), cancellationToken).ConfigureAwait(false);
                        break;
                    case ATYP_IPV6:
                        await stream.ReadAsync(buffer.AsMemory(0, 16), cancellationToken).ConfigureAwait(false);
                        break;
                    case ATYP_DOMAIN_NAME:
                        await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
                        addressLength = buffer[0];
                        await stream.ReadAsync(buffer.AsMemory(0, addressLength), cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new Exception("Unknown address type");
                }
                await stream.ReadAsync(buffer.AsMemory(0, 2), cancellationToken).ConfigureAwait(false);
                // response address not used
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static async ValueTask EstablishSocks4TunnelAsync(Stream stream, bool isVersion4a, string host, int port, Uri proxyUri, ICredentials? proxyCredentials, CancellationToken cancellationToken)
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

                if (isVersion4a)
                {
                    buffer[4] = 0;
                    buffer[5] = 0;
                    buffer[6] = 0;
                    buffer[7] = 255;
                }
                else
                {
                    bool addressWritten = false;
                    foreach (var address in await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false))
                    {
                        // SOCKS4 supports only IPv4
                        if (address.AddressFamily == Sockets.AddressFamily.InterNetwork)
                        {
                            address.TryWriteBytes(buffer.AsSpan(4), out int bytesWritten);
                            Debug.Assert(bytesWritten == 4);
                            addressWritten = true;
                            break;
                        }
                    }
                    if (!addressWritten)
                    {
                        throw new Exception("No suitable IPv4 address.");
                    }
                }

                int uLen = Encoding.UTF8.GetBytes(username, buffer.AsSpan(8));
                buffer[8 + uLen] = 0;
                int totalLength = 9 + uLen;

                if (isVersion4a)
                {
                    // https://www.openssh.com/txt/socks4a.protocol
                    int aLen = Encoding.UTF8.GetBytes(host, buffer.AsSpan(9 + uLen));
                    buffer[9 + uLen + aLen] = 0;
                    totalLength += aLen + 1;
                }

                await stream.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);

                // +----+----+----+----+----+----+----+----+
                // | VN | CD | DSTPORT |      DSTIP        |
                // +----+----+----+----+----+----+----+----+
                //    1    1      2              4
                await stream.ReadAsync(buffer.AsMemory(0, 8), cancellationToken).ConfigureAwait(false);
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
    }
}
