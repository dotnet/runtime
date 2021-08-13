// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class DeflateHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            string responseBody = "Sending DEFLATE compressed";

            context.Response.Headers.Add("Content-MD5", Convert.ToBase64String(ContentHelper.ComputeMD5Hash(responseBody)));
            context.Response.Headers.Add("Content-Encoding", "deflate");

            context.Response.ContentType = "text/plain";

            byte[] bytes = ContentHelper.GetDeflateBytes(responseBody);
            await context.Response.Body.WriteAsync(bytes);
        }
    }
}
