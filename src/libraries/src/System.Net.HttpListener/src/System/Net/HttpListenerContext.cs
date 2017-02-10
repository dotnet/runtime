// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerContext
    {
        internal HttpListener _listener;
        private HttpListenerResponse _response;
        private IPrincipal _user;

        public HttpListenerRequest Request { get; }

        public IPrincipal User => _user;

        public HttpListenerResponse Response
        {
            get
            {
                if (_response == null)
                {
                    _response = new HttpListenerResponse(this);
                }

                return _response;
            }
        }
    }
}
