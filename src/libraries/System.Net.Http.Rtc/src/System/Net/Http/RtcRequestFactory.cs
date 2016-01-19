// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Net.Http
{
    public static class RtcRequestFactory
    {
        public static HttpRequestMessage Create(HttpMethod method, Uri uri)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
