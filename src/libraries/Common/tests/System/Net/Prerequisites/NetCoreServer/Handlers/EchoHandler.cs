// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetCoreServer
{
    public class EchoHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            RequestHelper.AddResponseCookies(context);

            if (!AuthenticationHelper.HandleAuthentication(context))
            {
                return;
            }

            // Add original request method verb as a custom response header.
            context.Response.Headers["X-HttpRequest-Method"] = context.Request.Method;

            // Echo back JSON encoded payload.
            RequestInformation info = await RequestInformation.CreateAsync(context.Request);
            string echoJson = info.SerializeToJson();

            byte[] bytes = Encoding.UTF8.GetBytes(echoJson);

            // Compute MD5 hash so that clients can verify the received data.
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(bytes);
                string encodedHash = Convert.ToBase64String(hash);

                context.Response.Headers["Content-MD5"] = encodedHash;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength = bytes.Length;
            }

            if (context.Request.QueryString.HasValue && context.Request.QueryString.Value.Contains("delay10sec"))
            {
                await context.Response.StartAsync(CancellationToken.None);
                await context.Response.Body.FlushAsync();

                await Task.Delay(10000);
            }
            else if (context.Request.QueryString.HasValue && context.Request.QueryString.Value.Contains("delay1sec"))
            {
                await context.Response.StartAsync(CancellationToken.None);
                await Task.Delay(1000);
            }
            
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
