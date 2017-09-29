// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.Security
{
    public struct SslApplicationProtocol : IEquatable<SslApplicationProtocol>
    {
        private readonly byte[] _protocol;
        private readonly ReadOnlyMemory<byte> _readOnlyProtocol;
        private static readonly Encoding s_utf8 = Encoding.GetEncoding(Encoding.UTF8.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        // h2
        public static readonly SslApplicationProtocol Http2 = new SslApplicationProtocol(new byte[] { 0x68, 0x32 }, false);
        // http/1.1
        public static readonly SslApplicationProtocol Http11 = new SslApplicationProtocol(new byte[] { 0x68, 0x74, 0x74, 0x70, 0x2f, 0x31, 0x2e, 0x31 }, false);

        internal SslApplicationProtocol(byte[] protocol, bool copy)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            if (protocol.Length == 0)
            {
                throw new ArgumentException(SR.net_ssl_app_protocol_empty, nameof(protocol));
            }

            if (copy)
            {
                _protocol = new byte[protocol.Length];
                Array.Copy(protocol, _protocol, protocol.Length);
            }
            else
            {
                _protocol = protocol;
            }

            _readOnlyProtocol = new ReadOnlyMemory<byte>(_protocol);
        }

        public SslApplicationProtocol(byte[] protocol) : this(protocol, true) { }

        public SslApplicationProtocol(string protocol) : this(s_utf8.GetBytes(protocol), false) { }

        public ReadOnlyMemory<byte> Protocol
        {
            get => _readOnlyProtocol;
        }

        public bool Equals(SslApplicationProtocol other)
        {
            if (_protocol == null && other._protocol == null)
                return true;

            if (_protocol == null || other._protocol == null)
                return false;

            if (_protocol.Length != other._protocol.Length)
                return false;

            for (int i = 0; i < _protocol.Length; i++)
            {
                if (_protocol[i] != other._protocol[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is SslApplicationProtocol protocol)
            {
                return Equals(protocol);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _protocol.Length; i++)
            {
                hash = ((hash << 5) + hash) ^ _protocol[i];
            }

            return hash;
        }

        public override string ToString()
        {
            try
            {
                if (_protocol == null)
                {
                    return null;
                }

                return s_utf8.GetString(_protocol);
            }
            catch
            {
                // In case of decoding errors, return the byte values as hex string.
                int byteCharsLength = _protocol.Length * 5;
                char[] byteChars = new char[byteCharsLength];
                int index = 0;

                for (int i = 0; i < byteCharsLength; i += 5)
                {
                    byte b = _protocol[index++];
                    byteChars[i] = '0';
                    byteChars[i + 1] = 'x';
                    byteChars[i + 2] = GetHexValue(b / 16);
                    byteChars[i + 3] = GetHexValue(b % 16);
                    byteChars[i + 4] = ' ';
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

