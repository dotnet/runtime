﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;
using BlazorHosted.Server.Hubs;

// to avoid The WebRootPath was not found:
// C:\helix\work\workitem\e\wbt artifacts\SignalRClientTests_w5t2nznu_yll\BlazorHosted.Server\wwwroot. Static files may be unavailable.
var contentRootPath = AppContext.BaseDirectory;
var blazorClientPath = Path.Combine(contentRootPath, "../../../../BlazorHosted.Client");

var options = new WebApplicationOptions
{
    ContentRootPath = blazorClientPath
};

var builder = WebApplication.CreateBuilder(options);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.Zero; // minimize keep-alive messages
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add headers to enable SharedArrayBuffer
app.Use(async (context, next) =>
{
    var response = context.Response;
    response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");

    await next();
});
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
// var customWwwRoot = Path.Combine(app.Environment.ContentRootPath, "../BlazorHosted.Client/wwwroot");
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider(customWwwRoot),
//     RequestPath = "/customwwwroot"
// });

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.MapHub<ChatHub>("/chathub");

var environment = app.Services.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;

// Print the WebRootPath
Console.WriteLine($"WebRootPath: {environment?.WebRootPath}");

app.Run();
