// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    // TODO: #13187
    public sealed unsafe partial class HttpListenerRequest
    {
        internal ulong RequestId
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string[] AcceptTypes
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public long ContentLength64
        {
            get
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
        }

        public NameValueCollection Headers
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string HttpMethod
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Stream InputStream
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IsLocal
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IsSecureConnection
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool IsWebSocketRequest
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string RawUrl
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string ServiceName
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Uri Url
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public Uri UrlReferrer
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string UserAgent
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string UserHostAddress
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string UserHostName
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public string[] UserLanguages
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public int ClientCertificateError
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public X509Certificate2 GetClientCertificate()
        {
            throw new PlatformNotSupportedException();
        }

        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            throw new PlatformNotSupportedException();
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            throw new PlatformNotSupportedException();
        }

        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            throw new PlatformNotSupportedException();
        }

        public TransportContext TransportContext
        {
            get
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
        }

        public Version ProtocolVersion
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public bool HasEntityBody
        {
            get
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
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
