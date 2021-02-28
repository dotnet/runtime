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
    // https://tools.ietf.org/html/rfc1928
    internal static class SocksHelper
    {
        // socks protocol limits address length to 1 byte, thus the maximum possible buffer is 256+other fields
        private const int BufferSize = 512;
        private const int ProtocolVersion = 5;
        private const byte METHOD_NO_AUTH = 0;
        // private const byte METHOD_GSSAPI = 1;
        private const byte METHOD_USERNAME_PASSWORD = 2;
        private const byte METHOD_NO_ACCEPTABLE = 0xFF;
        private const byte CMD_CONNECT = 1;
        // private const byte CMD_BIND = 2;
        // private const byte CMD_UDP_ASSOCIATE = 3;
        // private const byte ATYP_IPV4 = 1;
        private const byte ATYP_DOMAIN_NAME = 3;
        // private const byte ATYP_IPV6 = 4;
        private const byte REP_SUCCESS = 0;
        // private const byte REP_FAILURE = 1;
        // private const byte REP_NOT_ALLOWED = 2;
        // private const byte REP_NETWORK_UNREACHABLE = 3;
        // private const byte REP_HOST_UNREACHABLE = 4;
        // private const byte REP_CONNECTION_REFUSED = 5;
        // private const byte REP_TTL_EXPIRED = 6;
        // private const byte REP_CMD_NOT_SUPPORT = 7;
        // private const byte REP_ATYP_NOT_SUPPORT = 8;

        public static async ValueTask EstablishSocks5TunnelAsync(Stream stream, string host, int port, Uri proxyUri, ICredentials? proxyCredentials, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                // +----+----------+----------+
                // |VER | NMETHODS | METHODS  |
                // +----+----------+----------+
                // | 1  |    1     | 1 to 255 |
                // +----+----------+----------+
                buffer[0] = ProtocolVersion;
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
                if (buffer[0] != ProtocolVersion)
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
                            buffer[0] = ProtocolVersion;
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
                            if (buffer[0] != ProtocolVersion)
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
                buffer[0] = ProtocolVersion;
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
                await stream.ReadAsync(buffer.AsMemory(0, 5), cancellationToken).ConfigureAwait(false);
                if (buffer[0] != ProtocolVersion)
                    throw new Exception("Bad protocol version");
                if (buffer[1] != REP_SUCCESS)
                    throw new Exception("Connection failed");
                addressLength = buffer[4];
                await stream.ReadAsync(buffer.AsMemory(0, addressLength + 2), cancellationToken).ConfigureAwait(false);
                // response address not used
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
