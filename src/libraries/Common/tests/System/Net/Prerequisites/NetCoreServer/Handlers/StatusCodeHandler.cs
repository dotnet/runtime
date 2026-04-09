// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace NetCoreServer
{
    public class StatusCodeHandler
    {
        public static async Task InvokeAsync(HttpContext context)
        {
            string statusCodeString = context.Request.Query["statuscode"];
            string statusDescription = context.Request.Query["statusdescription"];
            string delayString = context.Request.Query["delay"];
            try
            {
                int statusCode = int.Parse(statusCodeString);
                int delay = string.IsNullOrWhiteSpace(delayString) ? 0 : int.Parse(delayString);

                context.Response.StatusCode = statusCode;
                context.Response.SetStatusDescription(string.IsNullOrWhiteSpace(statusDescription) ? " " : statusDescription);

                if (delay > 0)
                {
                    var buffer = new byte[1];
                    if (context.Request.Method == HttpMethod.Post.Method)
                    {
                        await context.Request.Body.ReadExactlyAsync(buffer, CancellationToken.None);
                    }
                    await context.Response.StartAsync(CancellationToken.None);
                    await Task.Delay(delay);
                }
            }
            catch (Exception)
            {
                context.Response.StatusCode = 400;
                context.Response.SetStatusDescription("Error parsing statuscode: " + statusCodeString);
            }
        }
    }
}
