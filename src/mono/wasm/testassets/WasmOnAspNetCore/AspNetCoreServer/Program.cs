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

var client = builder.Configuration.GetValue<string>("client")?.ToLowerInvariant();
if (string.IsNullOrEmpty(client))
    throw new Exception($"Client arg cannot be empty. Choose: blazor or wasmbrowser");
var staticFilesPath = Path.Combine(AppContext.BaseDirectory, $"publish/wwwroot/{client}");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
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

Console.WriteLine($"staticFilesPath: {staticFilesPath}");
switch (client)
{
    case "wasmbrowser":
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new PhysicalFileProvider(staticFilesPath)
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(staticFilesPath)
        });
        break;
    case "blazor":
        app.UseStaticFiles();
        app.Map("/blazor", blazorApp => blazorApp.UseBlazorFrameworkFiles());
        break;
    default:
        throw new Exception($"Expected client to be wasmbrowser or blazor, not {client}.");

}
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.MapHub<ChatHub>("/chathub");

app.Run();
