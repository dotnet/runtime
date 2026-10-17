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
var poisonClientAssemblyValue = Environment.GetEnvironmentVariable("BLAZOR_WEB_WASM_POISON_CLIENT_ASSEMBLY");
var remainingClientAssemblyPoisons = int.TryParse(poisonClientAssemblyValue, out var poisonCount)
    ? poisonCount
    : string.Equals(poisonClientAssemblyValue, "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
var poisonClientAssemblyLock = new Lock();

app.Use(async (context, next) =>
{
    var wasPoisoned = false;
    if (HttpMethods.IsGet(context.Request.Method) &&
        context.Request.Path.Value is { } path &&
        path.Contains("BlazorWebWasm.Client", StringComparison.OrdinalIgnoreCase) &&
        path.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase))
    {
        lock (poisonClientAssemblyLock)
        {
            if (remainingClientAssemblyPoisons > 0)
            {
                remainingClientAssemblyPoisons--;
                wasPoisoned = true;
            }
        }
    }

    if (wasPoisoned)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/wasm";
        context.Response.ContentLength = 0;
        context.Response.Headers.CacheControl = "public,max-age=3600";
    }
    else
    {
        await next.Invoke();
    }

    var logEntry = new BlazorWebWasmRequestLog(
        DateTime.UtcNow,
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        wasPoisoned
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
