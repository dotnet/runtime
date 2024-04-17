// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.HPack;
using System.Net.Http.QPack;
using System.Text;

namespace System.Net.Http
{
    public partial class HttpMethod
    {
        private byte[]? _http1EncodedBytes;
        private byte[]? _http2EncodedBytes;
        private byte[]? _http3EncodedBytes;
        private int _http3Index;

        internal bool MustHaveRequestBody { get; private set; }
        internal bool IsConnect { get; private set; }
        internal bool IsHead { get; private set; }

        partial void Initialize(string method)
        {
            Initialize(GetKnownMethod(method)?._http3Index ?? 0);
        }

        partial void Initialize(int http3Index)
        {
            _http3Index = http3Index;

            if (http3Index == H3StaticTable.MethodConnect)
            {
                IsConnect = true;
            }
            else if (http3Index == H3StaticTable.MethodHead)
            {
                IsHead = true;
            }
            else
            {
                MustHaveRequestBody = http3Index is not (H3StaticTable.MethodGet or H3StaticTable.MethodOptions or H3StaticTable.MethodDelete);
            }
        }

        internal byte[] Http1EncodedBytes => _http1EncodedBytes ?? CreateHttp1EncodedBytes();
        internal byte[] Http2EncodedBytes => _http2EncodedBytes ?? CreateHttp2EncodedBytes();
        internal byte[] Http3EncodedBytes => _http3EncodedBytes ?? CreateHttp3EncodedBytes();

        private byte[] CreateHttp1EncodedBytes()
        {
            HttpMethod? knownMethod = GetKnownMethod(Method);
            byte[]? bytes = knownMethod?._http1EncodedBytes;

            if (bytes is null)
            {
                Debug.Assert(Ascii.IsValid(Method));

                string method = knownMethod?.Method ?? Method;
                bytes = new byte[method.Length + 1];
                Ascii.FromUtf16(method, bytes, out _);
                bytes[^1] = (byte)' ';

                if (knownMethod is not null)
                {
                    knownMethod._http1EncodedBytes = bytes;
                }
            }

            _http1EncodedBytes = bytes;
            return bytes;
        }

        private byte[] CreateHttp2EncodedBytes()
        {
            HttpMethod? knownMethod = GetKnownMethod(Method);
            byte[]? bytes = knownMethod?._http2EncodedBytes;

            if (bytes is null)
            {
                bytes = _http3Index switch
                {
                    H3StaticTable.MethodGet => [0x80 | H2StaticTable.MethodGet],
                    H3StaticTable.MethodPost => [0x80 | H2StaticTable.MethodPost],
                    _ => HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(H2StaticTable.MethodGet, knownMethod?.Method ?? Method)
                };

                if (knownMethod is not null)
                {
                    knownMethod._http2EncodedBytes = bytes;
                }
            }

            _http2EncodedBytes = bytes;
            return bytes;
        }

        private byte[] CreateHttp3EncodedBytes()
        {
            HttpMethod? knownMethod = GetKnownMethod(Method);
            byte[]? bytes = knownMethod?._http3EncodedBytes;

            if (bytes is null)
            {
                bytes = _http3Index > 0
                    ? QPackEncoder.EncodeStaticIndexedHeaderFieldToArray(_http3Index)
                    : QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(H3StaticTable.MethodGet, knownMethod?.Method ?? Method);

                if (knownMethod is not null)
                {
                    knownMethod._http3EncodedBytes = bytes;
                }
            }

            _http3EncodedBytes = bytes;
            return bytes;
        }
    }
}
