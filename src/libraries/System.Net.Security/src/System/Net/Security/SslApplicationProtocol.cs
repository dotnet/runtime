// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.Security
{
    public readonly struct SslApplicationProtocol : IEquatable<SslApplicationProtocol>
    {
        private readonly ReadOnlyMemory<byte> _readOnlyProtocol;
        private static readonly Encoding s_utf8 = Encoding.GetEncoding(Encoding.UTF8.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        // Refer IANA on ApplicationProtocols: https://www.iana.org/assignments/tls-extensiontype-values/tls-extensiontype-values.xhtml#alpn-protocol-ids
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

            // RFC 7301 states protocol size <= 255 bytes.
            if (protocol.Length == 0 || protocol.Length > 255)
            {
                throw new ArgumentException(SR.net_ssl_app_protocol_invalid, nameof(protocol));
            }

            if (copy)
            {
                byte[] temp = new byte[protocol.Length];
                Array.Copy(protocol, 0, temp, 0, protocol.Length);
                _readOnlyProtocol = new ReadOnlyMemory<byte>(temp);
            }
            else
            {
                _readOnlyProtocol = new ReadOnlyMemory<byte>(protocol);
            }
        }

        public SslApplicationProtocol(byte[] protocol) : this(protocol, true) { }

        public SslApplicationProtocol(string protocol) : this(s_utf8.GetBytes(protocol), copy: false) { }

        public ReadOnlyMemory<byte> Protocol
        {
            get => _readOnlyProtocol;
        }

        public bool Equals(SslApplicationProtocol other)
        {
            if (_readOnlyProtocol.Length != other._readOnlyProtocol.Length)
                return false;

            return (_readOnlyProtocol.IsEmpty && other._readOnlyProtocol.IsEmpty) ||
                _readOnlyProtocol.Span.SequenceEqual(other._readOnlyProtocol.Span);
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
            if (_readOnlyProtocol.Length == 0)
                return 0;

            int hash1 = 0;
            ReadOnlySpan<byte> pSpan = _readOnlyProtocol.Span;
            for (int i = 0; i < _readOnlyProtocol.Length; i++)
            {
                hash1 = ((hash1 << 5) + hash1) ^ pSpan[i];
            }

            return hash1;
        }

        public override string ToString()
        {
            try
            {
                if (_readOnlyProtocol.Length == 0)
                {
                    return null;
                }

                return s_utf8.GetString(_readOnlyProtocol.Span);
            }
            catch
            {
                // In case of decoding errors, return the byte values as hex string.
                int byteCharsLength = _readOnlyProtocol.Length * 5;
                char[] byteChars = new char[byteCharsLength];
                int index = 0;

                ReadOnlySpan<byte> pSpan = _readOnlyProtocol.Span;
                for (int i = 0; i < byteCharsLength; i += 5)
                {
                    byte b = pSpan[index++];
                    byteChars[i] = '0';
                    byteChars[i + 1] = 'x';
                    byteChars[i + 2] = GetHexValue(Math.DivRem(b, 16, out int rem));
                    byteChars[i + 3] = GetHexValue(rem);
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

