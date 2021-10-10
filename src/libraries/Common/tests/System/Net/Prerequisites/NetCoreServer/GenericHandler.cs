// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NetCoreServer
{
    public class GenericHandler
    {
        // Must have constructor with this signature, otherwise exception at run time.
        public GenericHandler(RequestDelegate next)
        {
            // This catch all HTTP Handler, so no need to store next.
        }

        public async Task Invoke(HttpContext context)
        {
            PathString path = context.Request.Path;
            if (path.Equals(new PathString("/deflate.ashx")))
            {
                await DeflateHandler.InvokeAsync(context);
                return;
            }

            if (path.Equals(new PathString("/echo.ashx")))
            {
                await EchoHandler.InvokeAsync(context);
                return;
            }

            if (path.Equals(new PathString("/emptycontent.ashx")))
            {
                EmptyContentHandler.Invoke(context);
                return;
            }

            if (path.Equals(new PathString("/gzip.ashx")))
            {
                await GZipHandler.InvokeAsync(context);
                return;
            }

            if (path.Equals(new PathString("/redirect.ashx")))
            {
                RedirectHandler.Invoke(context);
                return;
            }

            if (path.Equals(new PathString("/statuscode.ashx")))
            {
                StatusCodeHandler.Invoke(context);
                return;
            }

            if (path.Equals(new PathString("/verifyupload.ashx")))
            {
                await VerifyUploadHandler.InvokeAsync(context);
                return;
            }

            if (path.Equals(new PathString("/version")))
            {
                await VersionHandler.InvokeAsync(context);
                return;
            }

            if (path.Equals(new PathString("/websocket/echowebsocket.ashx")))
            {
                await EchoWebSocketHandler.InvokeAsync(context);
                return;
            }

            if (path.Equals(new PathString("/websocket/echowebsocketheaders.ashx")))
            {
                await EchoWebSocketHeadersHandler.InvokeAsync(context);
                return;
            }
            if (path.Equals(new PathString("/test.ashx")))
            {
                await TestHandler.InvokeAsync(context);
                return;
            }

            // Default handling.
            await EchoHandler.InvokeAsync(context);
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
