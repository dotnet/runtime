// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add headers to enable SharedArrayBuffer
app.Use(async (context, next) =>
{
    var response = context.Response;
    response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");

    await next();
});

app.UseRouting();

app.MapHub<ChatHub>("/chathub");

// WASM app as a static file provider
var wasmPath = Path.Combine(app.Environment.ContentRootPath, "publish/wwwroot");
Console.WriteLine($"wasmPath: {wasmPath}");
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(wasmPath)
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wasmPath)
});

app.Run();
