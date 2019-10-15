// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Web;

namespace WebServer
{
    public class VerifyUpload : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            // Report back original request method verb.
            context.Response.Headers.Add("X-HttpRequest-Method", context.Request.HttpMethod);

            // Report back original entity-body related request headers.
            string contentLength = context.Request.Headers["Content-Length"];
            if (!string.IsNullOrEmpty(contentLength))
            {
                context.Response.Headers.Add("X-HttpRequest-Headers-ContentLength", contentLength);
            }

            string transferEncoding = context.Request.Headers["Transfer-Encoding"];
            if (!string.IsNullOrEmpty(transferEncoding))
            {
                context.Response.Headers.Add("X-HttpRequest-Headers-TransferEncoding", transferEncoding);
            }

            // Get expected MD5 hash of request body.
            string expectedHash = context.Request.Headers["Content-MD5"];
            if (string.IsNullOrEmpty(expectedHash))
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Missing 'Content-MD5' request header";
                return;
            }

            // Compute MD5 hash of received request body.
            string actualHash;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(ReadAllRequestBytes(context));
                actualHash = Convert.ToBase64String(hash);
            }

            if (expectedHash == actualHash)
            {
                context.Response.StatusCode = 200;
            }
            else
            {
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = "Request body not verfied";
            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        private static byte[] ReadAllRequestBytes(HttpContext context)
        {
            Stream requestStream = context.Request.GetBufferedInputStream();
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = requestStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
