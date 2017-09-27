// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.Security
{
    public struct SslApplicationProtocol : IEquatable<SslApplicationProtocol>
    {
        private ReadOnlyMemory<byte> _protocol;

        // h2
        public static readonly SslApplicationProtocol Http2 = new SslApplicationProtocol(new ReadOnlyMemory<byte>(new byte[] { 0x68, 0x32 }));
        // http/1.1
        public static readonly SslApplicationProtocol Http11 = new SslApplicationProtocol(new ReadOnlyMemory<byte>(new byte[] { 0x68, 0x74, 0x74, 0x70, 0x2f, 0x31, 0x2e, 0x31 }));

        internal SslApplicationProtocol(ReadOnlyMemory<byte> protocol)
        {
            _protocol = protocol;
        }

        public SslApplicationProtocol(byte[] protocol)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            if (protocol.Length == 0)
            {
                throw new ArgumentException(SR.net_ssl_app_protocol_empty, nameof(protocol));
            }

            byte[] temp = new byte[protocol.Length];
            Array.Copy(protocol, temp, protocol.Length);

            _protocol = new ReadOnlyMemory<byte>(temp);
        }

        public SslApplicationProtocol(string protocol)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            if (protocol.Length == 0)
            {
                throw new ArgumentException(SR.net_ssl_app_protocol_empty, nameof(protocol));
            }

            _protocol = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(protocol));
        }
        
        public ReadOnlyMemory<byte> Protocol => _protocol;

        public bool Equals(SslApplicationProtocol other)
        {
            return _protocol.Equals(other._protocol);
        }

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyMemory<byte> protocol)
            {
                return _protocol.Equals(protocol);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _protocol.GetHashCode();
        }

        public override string ToString()
        {
            try
            {
                return Encoding.UTF8.GetString(_protocol.ToArray());
            }
            catch
            {
                // In case of decoding errors, return the byte values as hex string.
                byte[] temp = _protocol.ToArray();
                int byteCharsLength = temp.Length * 3;
                char[] byteChars = new char[byteCharsLength];
                int index = 0;

                for (int i = 0; i < byteCharsLength; i += 3)
                {
                    byte b = temp[index++];
                    byteChars[i] = GetHexValue(b / 16);
                    byteChars[i + 1] = GetHexValue(b % 16);
                    byteChars[i + 2] = ' ';
                }

                return new string(byteChars, 0, byteCharsLength - 1);

                char GetHexValue(int i)
                {
                    if (i < 10)
                        return (char)(i + '0');

                    return (char)(i - 10 + 'a');
                }
            }
        }

        public static bool operator ==(SslApplicationProtocol left, SslApplicationProtocol right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SslApplicationProtocol left, SslApplicationProtocol right)
        {
            return !(left == right);
        }
    }
}

