// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Text;

namespace System.Net.Security
{
    public readonly struct SslApplicationProtocol : IEquatable<SslApplicationProtocol>
    {
        private static readonly Encoding s_utf8 = Encoding.GetEncoding(Encoding.UTF8.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        private static readonly byte[] s_http2Utf8 = new byte[] { 0x68, 0x32 }; // "h2"
        private static readonly byte[] s_http11Utf8 = new byte[] { 0x68, 0x74, 0x74, 0x70, 0x2f, 0x31, 0x2e, 0x31 }; // "http/1.1"

        // Refer to IANA on ApplicationProtocols: https://www.iana.org/assignments/tls-extensiontype-values/tls-extensiontype-values.xhtml#alpn-protocol-ids
        // h2
        public static readonly SslApplicationProtocol Http2 = new SslApplicationProtocol(s_http2Utf8, copy: false);
        // http/1.1
        public static readonly SslApplicationProtocol Http11 = new SslApplicationProtocol(s_http11Utf8, copy: false);

        private readonly byte[] _readOnlyProtocol;

        internal SslApplicationProtocol(byte[] protocol, bool copy)
        {
            Debug.Assert(protocol != null);

            // RFC 7301 states protocol size <= 255 bytes.
            if (protocol.Length == 0 || protocol.Length > 255)
            {
                throw new ArgumentException(SR.net_ssl_app_protocol_invalid, nameof(protocol));
            }

            _readOnlyProtocol = copy ?
                protocol.AsSpan().ToArray() :
                protocol;
        }

        public SslApplicationProtocol(byte[] protocol) :
            this(protocol ?? throw new ArgumentNullException(nameof(protocol)), copy: true)
        {
        }

        public SslApplicationProtocol(string protocol) :
            this(s_utf8.GetBytes(protocol ?? throw new ArgumentNullException(nameof(protocol))), copy: false)
        {
        }

        public ReadOnlyMemory<byte> Protocol => _readOnlyProtocol;

        public bool Equals(SslApplicationProtocol other) =>
            ((ReadOnlySpan<byte>)_readOnlyProtocol).SequenceEqual(other._readOnlyProtocol);

        public override bool Equals(object? obj) => obj is SslApplicationProtocol protocol && Equals(protocol);

        public override int GetHashCode()
        {
            byte[] arr = _readOnlyProtocol;
            if (arr == null)
            {
                return 0;
            }

            int hash = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                hash = ((hash << 5) + hash) ^ arr[i];
            }

            return hash;
        }

        public override string ToString()
        {
            byte[] arr = _readOnlyProtocol;
            try
            {
                return
                    arr is null ? string.Empty :
                    ReferenceEquals(arr, s_http2Utf8) ? "h2" :
                    ReferenceEquals(arr, s_http11Utf8) ? "http/1.1" :
                    s_utf8.GetString(arr);
            }
            catch
            {
                // In case of decoding errors, return the byte values as hex string.
                char[] byteChars = new char[arr.Length * 5];
                int index = 0;

                for (int i = 0; i < byteChars.Length; i += 5)
                {
                    byte b = arr[index++];
                    byteChars[i] = '0';
                    byteChars[i + 1] = 'x';
                    byteChars[i + 2] = HexConverter.ToCharLower(b >> 4);
                    byteChars[i + 3] = HexConverter.ToCharLower(b);
                    byteChars[i + 4] = ' ';
                }

                return new string(byteChars, 0, byteChars.Length - 1);
            }
        }

        public static bool operator ==(SslApplicationProtocol left, SslApplicationProtocol right) =>
            left.Equals(right);

        public static bool operator !=(SslApplicationProtocol left, SslApplicationProtocol right) =>
            !(left == right);
    }
}
