// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class GZipHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            string responseBody = "Sending GZIP compressed";

            context.Response.Headers.Add("Content-MD5", Convert.ToBase64String(ContentHelper.ComputeMD5Hash(responseBody)));
            context.Response.Headers.Add("Content-Encoding", "gzip");

            context.Response.ContentType = "text/plain";

            byte[] bytes = ContentHelper.GetGZipBytes(responseBody);
            await context.Response.Body.WriteAsync(bytes);
        }
    }
}
