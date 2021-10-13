// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class LargeResponseHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            RequestHelper.AddResponseCookies(context);

            if (!AuthenticationHelper.HandleAuthentication(context))
            {
                return;
            }

            // Add original request method verb as a custom response header.
            context.Response.Headers.Add("X-HttpRequest-Method", context.Request.Method);

            var size = 1024;
            if (context.Request.Query.TryGetValue("size", out var value))
            {
                size = Int32.Parse(value);
            }

            var bytes =new byte[size];
            Random.Shared.NextBytes(bytes);

            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength = bytes.Length;
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
