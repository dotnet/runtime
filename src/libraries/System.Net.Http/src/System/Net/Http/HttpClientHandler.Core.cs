// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        // This partial implementation contains members common to Windows and Unix running on .NET Core.

        public long MaxRequestContentBufferSize
        {
            // This property has been deprecated. In the .NET Desktop it was only used when the handler needed to 
            // automatically buffer the request content. That only happened if neither 'Content-Length' nor 
            // 'Transfer-Encoding: chunked' request headers were specified. So, the handler thus needed to buffer
            // in the request content to determine its length and then would choose 'Content-Length' semantics when
            // POST'ing. In CoreCLR and .NETNative, the handler will resolve the ambiguity by always choosing
            // 'Transfer-Encoding: chunked'. The handler will never automatically buffer in the request content.
            get { return 0; }

            // TODO (#7879): Add message/link to exception explaining the deprecation. 
            // Update corresponding exception in HttpClientHandler.Unix.cs if/when this is updated.
            set { throw new PlatformNotSupportedException(); }
        }
    }
}
