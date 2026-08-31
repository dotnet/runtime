// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace RemoteLoopServer
{
    public class GenericHandler
    {
        RequestDelegate _next;
        ILogger _logger;
        public GenericHandler(RequestDelegate next, ILogger logger)
        {
            this._next = next;
            this._logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            PathString path = context.Request.Path;
            if (path.Equals(new PathString("/RemoteLoop")))
            {
                await RemoteLoopHandler.InvokeAsync(context, _logger);
                return;
            }

            await _next(context);
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
