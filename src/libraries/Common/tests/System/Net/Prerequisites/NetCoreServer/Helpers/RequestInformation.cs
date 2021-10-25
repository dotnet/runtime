// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace NetCoreServer
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

        public X509Certificate2 ClientCertificate { get; private set; }

        public static async Task<RequestInformation> CreateAsync(HttpRequest request)
        {
            var info = new RequestInformation();
            info.Method = request.Method;
            info.Url = request.Path + request.QueryString;
            info.Headers = new NameValueCollection();
            foreach (KeyValuePair<string, StringValues> header in request.Headers)
            {
                info.Headers.Add(header.Key, header.Value.ToString());
            }

            var cookies = new NameValueCollection();
            CookieCollection cookieCollection = RequestHelper.GetRequestCookies(request);
            foreach (Cookie cookie in cookieCollection)
            {
                cookies.Add(cookie.Name, cookie.Value);
            }
            info.Cookies = cookies;

            string body = string.Empty;
            try
            {
                Stream stream = request.Body;
                using (var reader = new StreamReader(stream))
                {
                    body = await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                // We might want to log these exceptions also.
                body = ex.ToString();
            }
            finally
            {
                info.BodyContent = body;
                info.BodyLength = body.Length;
            }

            info.SecureConnection = request.IsHttps;

            // FixMe: https://github.com/dotnet/runtime/issues/52693
            // info.ClientCertificate = request.ClientCertificate;

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
