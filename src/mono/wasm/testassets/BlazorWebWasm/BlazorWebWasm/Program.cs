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
var cacheTestFailures = (Environment.GetEnvironmentVariable("BLAZOR_CACHE_TEST_FAILURES") ?? "").Split(',');
var cacheTestAsset = Environment.GetEnvironmentVariable("BLAZOR_CACHE_TEST_ASSET");
var cacheTestRequest = -1;

app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method) &&
        context.Request.Path.Value is { } path &&
        cacheTestAsset is not null &&
        path.EndsWith("/" + cacheTestAsset, StringComparison.Ordinal) &&
        Interlocked.Increment(ref cacheTestRequest) is var index &&
        index < cacheTestFailures.Length &&
        cacheTestFailures[index] is { Length: > 0 } failure)
    {
        context.Response.Headers.CacheControl = "public,max-age=3600";
        context.Response.ContentType = "application/wasm";
        context.Response.StatusCode = failure switch
        {
            "empty" or "integrity" => StatusCodes.Status200OK,
            "not-found" => StatusCodes.Status404NotFound,
            "unavailable" => StatusCodes.Status503ServiceUnavailable,
            _ => throw new InvalidOperationException($"Unknown cache test failure '{failure}'.")
        };
        byte[] body = failure is "integrity" ? [0] : [];
        context.Response.ContentLength = body.Length;
        await context.Response.Body.WriteAsync(body);
    }
    else
    {
        await next.Invoke();
    }
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

app.MapGet("/cache-seed", () => Results.Content(
    """
    <!DOCTYPE html>
    <html>
    <body>
    <main class="page">Seeding the HTTP cache</main>
    <script>
        (async () => {
            const asset = new URLSearchParams(location.search).get("asset");
            const response = await fetch(`/_framework/${asset}`, { cache: "reload" });
            await response.arrayBuffer();
            document.title = "cache-seeded";
        })();
    </script>
    </body>
    </html>
    """, "text/html"));

app.Run();
