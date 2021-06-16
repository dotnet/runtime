// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.QPack;
using System.Threading;

namespace System.Net.Http
{
    public partial class HttpMethod
    {
        private byte[]? _http3EncodedBytes;

        internal byte[] Http3EncodedBytes
        {
            get
            {
                byte[]? http3EncodedBytes = Volatile.Read(ref _http3EncodedBytes);
                if (http3EncodedBytes is null)
                {
                    Volatile.Write(ref _http3EncodedBytes, http3EncodedBytes = _http3Index is int index && index >= 0 ?
                        QPackEncoder.EncodeStaticIndexedHeaderFieldToArray(index) :
                        QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(H3StaticTable.MethodGet, _method));
                }

                return http3EncodedBytes;
            }
        }
    }
}
