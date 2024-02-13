// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;
using BlazorHosted.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

 // Add command-line arguments to configuration to pass wbt LogRootPath that is availabe in Azure
var configuration = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

var logRootPath = configuration["logRootPath"] ?? "logs";

// Add file logging
builder.Logging.AddFile(logRootPath + "/myapp-{Date}.txt");

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

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

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.MapHub<ChatHub>("/chathub");

app.Run();
