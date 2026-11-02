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
var injectClientAssemblyFailureValue = Environment.GetEnvironmentVariable("BLAZOR_WEB_WASM_INJECT_CLIENT_ASSEMBLY_FAILURE");
var remainingClientAssemblyFailures = int.TryParse(injectClientAssemblyFailureValue, out var failureCount)
    ? failureCount
    : string.Equals(injectClientAssemblyFailureValue, "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
var clientAssemblyFailureMode = Environment.GetEnvironmentVariable("BLAZOR_WEB_WASM_CLIENT_ASSEMBLY_FAILURE_MODE") ?? "empty";
var clientAssemblyFailureLock = new Lock();

app.Use(async (context, next) =>
{
    var wasFailureInjected = false;
    if (HttpMethods.IsGet(context.Request.Method) &&
        context.Request.Path.Value is { } path &&
        path.Contains("BlazorWebWasm.Client", StringComparison.OrdinalIgnoreCase) &&
        path.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase))
    {
        lock (clientAssemblyFailureLock)
        {
            if (remainingClientAssemblyFailures > 0)
            {
                remainingClientAssemblyFailures--;
                wasFailureInjected = true;
            }
        }
    }

    if (wasFailureInjected)
    {
        context.Response.ContentType = "application/wasm";
        context.Response.Headers.CacheControl = "public,max-age=3600";
        switch (clientAssemblyFailureMode)
        {
            case "empty":
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentLength = 0;
                break;
            case "error":
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentLength = 0;
                break;
            case "integrity":
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentLength = 1;
                await context.Response.Body.WriteAsync(new byte[] { 0 });
                break;
            default:
                throw new InvalidOperationException($"Unknown client assembly failure mode '{clientAssemblyFailureMode}'.");
        }
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
        wasFailureInjected
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
app.MapGet("/cache-seed", () => Results.Content(
    """
    <!DOCTYPE html>
    <html>
    <body>
    <main class="page">Seeding cache...</main>
    <script>
    (async () => {
        const assemblyName = new URLSearchParams(location.search).get('asset');
        if (!assemblyName) {
            throw new Error('The client assembly was not specified.');
        }

        await fetch(`/_framework/${assemblyName}`);
        document.title = 'cache-seeded';
    })();
    </script>
    </body>
    </html>
    """,
    "text/html"));

app.Run();
