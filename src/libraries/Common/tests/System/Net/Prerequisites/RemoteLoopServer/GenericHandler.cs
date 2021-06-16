// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace RemoteLoopServer
{
    public class GenericHandler
    {
        RequestDelegate next;
        public GenericHandler(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            PathString path = context.Request.Path;
            if (path.Equals(new PathString("/remoteLoop")))
            {
                await RemoteLoopHandler.InvokeAsync(context);
                return;
            }

            await next(context);
        }
    }

    public static class GenericHandlerExtensions
    {
        public static IApplicationBuilder UseGenericHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GenericHandler>();
        }

        public static void SetStatusDescription(this HttpResponse response, string description)
        {
            response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = description;
        }
    }
}
