// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;
using BlazorHosted.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

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

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.MapHub<ChatHub>("/chathub");

app.Run();
