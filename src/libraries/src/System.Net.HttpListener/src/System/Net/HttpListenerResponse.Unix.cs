// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

namespace System.Net
{
    // TODO: #13187
    public sealed unsafe partial class HttpListenerResponse
    {
        public Encoding ContentEncoding
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string ContentType
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Stream OutputStream
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string RedirectLocation
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public int StatusCode
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string StatusDescription
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            throw new PlatformNotSupportedException();
        }

        public bool SendChunked
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool KeepAlive
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public void AddHeader(string name, string value)
        {
            throw new PlatformNotSupportedException();
        }

        public void AppendHeader(string name, string value)
        {
            throw new PlatformNotSupportedException();
        }

        public void Redirect(string url)
        {
            throw new PlatformNotSupportedException();
        }

        public void AppendCookie(Cookie cookie)
        {
            throw new PlatformNotSupportedException();
        }

        public void SetCookie(Cookie cookie)
        {
            throw new PlatformNotSupportedException();
        }

        public long ContentLength64
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Version ProtocolVersion
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public void Abort()
        {
            throw new PlatformNotSupportedException();
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            throw new PlatformNotSupportedException();
        }

        private void Dispose(bool disposing)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
