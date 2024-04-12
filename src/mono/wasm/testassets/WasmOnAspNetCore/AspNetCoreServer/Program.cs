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

var client = builder.Configuration.GetValue<string>("client")?.ToLowerInvariant();
if (string.IsNullOrEmpty(client))
    throw new Exception($"Client arg cannot be empty. Choose: blazor or wasmbrowser");

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
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add headers to enable SharedArrayBuffer
app.Use(async (context, next) =>
{
    var response = context.Response;
    response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");

    await next();
});

switch (client)
{
    case "wasmbrowser":
        var staticFilesPath = Path.Combine(AppContext.BaseDirectory, $"publish/wwwroot/{client}");
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new PhysicalFileProvider(staticFilesPath)
        });
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".dll"] = "application/octet-stream";
        provider.Mappings[".dat"] = "application/octet-stream";
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = provider,
            FileProvider = new PhysicalFileProvider(staticFilesPath)
        });
        break;
    case "blazor":
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
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
