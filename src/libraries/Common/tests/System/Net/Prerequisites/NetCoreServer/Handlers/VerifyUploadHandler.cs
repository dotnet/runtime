// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NetCoreServer
{
    public class VerifyUploadHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;

            // Report back original request method verb.
            context.Response.Headers["X-HttpRequest-Method"] = context.Request.Method;

            // Report back original entity-body related request headers.
            string contentLength = context.Request.Headers["Content-Length"];
            if (!string.IsNullOrEmpty(contentLength))
            {
                context.Response.Headers["X-HttpRequest-Headers-ContentLength"] = contentLength;
            }

            string transferEncoding = context.Request.Headers["Transfer-Encoding"];
            if (!string.IsNullOrEmpty(transferEncoding))
            {
                context.Response.Headers["X-HttpRequest-Headers-TransferEncoding"] = transferEncoding;
            }

            // Compute MD5 hash of received request body.
            (byte[] md5Bytes, int bodyLength) = await ComputeMD5HashRequestBodyAsync(context);

            // Report back the actual body length.
            context.Response.Headers["X-HttpRequest-Body-Length"] = bodyLength.ToString();

            // Skip MD5 checksum for empty request body
            // or for requests which opt to skip it due to [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
            if (bodyLength == 0 || !string.IsNullOrEmpty(context.Request.Headers["Content-MD5-Skip"]))
            {
                context.Response.StatusCode = 200;
                return;
            }

            // Get expected MD5 hash of request body.
            string expectedHash = context.Request.Headers["Content-MD5"];
            if (string.IsNullOrEmpty(expectedHash))
            {
                context.Response.StatusCode = 400;
                context.Response.SetStatusDescription("Missing 'Content-MD5' request header");
                return;
            }

            string actualHash = Convert.ToBase64String(md5Bytes);

            if (expectedHash == actualHash)
            {
                context.Response.StatusCode = 200;
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.SetStatusDescription("Received request body fails MD5 checksum");
            }
        }

        private static async Task<(byte[] MD5Hash, int BodyLength)> ComputeMD5HashRequestBodyAsync(HttpContext context)
        {
            Stream requestStream = context.Request.Body;
            byte[] buffer = new byte[16 * 1024];
            using (MD5 md5 = MD5.Create())
            {
                int read, size = 0;
                while ((read = await requestStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    size += read;
                    md5.TransformBlock(buffer, 0, read, buffer, 0);
                }
                md5.TransformFinalBlock(buffer, 0, read);
                return (md5.Hash, size);
            }
        }
    }
}
