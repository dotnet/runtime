// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();

// Add headers to enable SharedArrayBuffer
app.Use(async (context, next) =>
{
    var response = context.Response;
    response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");

    await next();
});

app.UseDefaultFiles();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".dll"] = "application/octet-stream";
provider.Mappings[".pdb"] = "application/octet-stream";
provider.Mappings[".dat"] = "application/octet-stream";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
});

ConfigureClientApp(app, "wasmclient");
ConfigureClientApp(app, "blazorclient");

app.Run();


static void ConfigureClientApp(WebApplication app, string clientAppPath)
{
    app.MapWhen(
        ctx => ctx.Request.Path.StartsWithSegments($"/{clientAppPath}", out var rest),
        clientApp =>
        {
            clientApp
                .UseBlazorFrameworkFiles($"/{clientAppPath}")
                .UsePathBase($"/{clientAppPath}")
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<ChatHub>("/chathub");
                    endpoints.MapFallbackToFile($"{clientAppPath}/index.html");
                });
        }
    );
}
