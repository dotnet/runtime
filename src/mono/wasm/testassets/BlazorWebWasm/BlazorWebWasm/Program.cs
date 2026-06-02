using BlazorWebWasm.Client.Pages;
using BlazorWebWasm.Components;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

var requestLogs = new List<BlazorWebWasmRequestLog>();
var requestLogsLock = new Lock();

app.Use(async (context, next) =>
{
    await next.Invoke();
    var logEntry = new BlazorWebWasmRequestLog(
        DateTime.UtcNow,
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode
    );
    lock (requestLogsLock)
    {
        requestLogs.Add(logEntry);
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorWebWasm.Client._Imports).Assembly);

app.MapGet("/request-logs", () =>
{
    lock (requestLogsLock)
    {
        return requestLogs.ToList();
    }
});
app.MapDelete("/request-logs", () =>
{
    lock (requestLogsLock)
    {
        requestLogs.Clear();
    }
});

app.Run();
