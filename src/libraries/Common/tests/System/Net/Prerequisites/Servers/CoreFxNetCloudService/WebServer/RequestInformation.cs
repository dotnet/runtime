// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

using Newtonsoft.Json;

namespace WebServer
{
    public class RequestInformation
    {
        public string Method { get; private set; }

        public string Url { get; private set; }

        public NameValueCollection Headers { get; private set; }

        public NameValueCollection Cookies { get; private set; }

        public string BodyContent { get; private set; }

        public int BodyLength { get; private set; }

        public bool SecureConnection { get; private set; }

        public bool ClientCertificatePresent { get; private set; }

        public HttpClientCertificate ClientCertificate { get; private set; }

        public static RequestInformation Create(HttpRequest request)
        {
            var info = new RequestInformation();
            info.Method = request.HttpMethod;
            info.Url = request.RawUrl;
            info.Headers = request.Headers;

            var cookies = new NameValueCollection();
            CookieCollection cookieCollection = RequestHelper.GetRequestCookies(request);
            foreach (Cookie cookie in cookieCollection)
            {
                cookies.Add(cookie.Name, cookie.Value);
            }
            info.Cookies = cookies;

            Stream stream = request.GetBufferedInputStream();
            using (var reader = new StreamReader(stream))
            {
                string body = reader.ReadToEnd();
                info.BodyContent = body;
                info.BodyLength = body.Length;
            }

            info.SecureConnection = request.IsSecureConnection;

            var cs = request.ClientCertificate;
            info.ClientCertificatePresent = cs.IsPresent;
            if (cs.IsPresent)
            {
                info.ClientCertificate = request.ClientCertificate;
            }

            return info;
        }

        public static RequestInformation DeSerializeFromJson(string json)
        {
            return (RequestInformation)JsonConvert.DeserializeObject(
                json,
                typeof(RequestInformation),
                new NameValueCollectionConverter());
        }

        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(this, new NameValueCollectionConverter());
        }

        private RequestInformation()
        {
        }
    }
}
