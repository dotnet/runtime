---
name: writing-extensions-code
description: "Guidance for writing and modifying Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Configuration, Microsoft.Extensions.Logging, Microsoft.Extensions.Hosting, Microsoft.Extensions.Caching, Microsoft.Extensions.Options, Microsoft.Extensions.Http, Microsoft.Extensions.FileProviders, Microsoft.Extensions.Primitives, and System.IO.Compression code in dotnet/runtime. Covers DI lifetime management, configuration binding, options validation, logging provider patterns, caching semantics, compression format compliance, and host lifecycle. For full code review, delegates to the @extensions-reviewer agent. Trigger words: IServiceCollection, IConfiguration, IConfigurationBuilder, ILogger, ILoggerFactory, IHost, IHostBuilder, IHostedService, IMemoryCache, IDistributedCache, IOptions, IOptionsMonitor, ZipArchive, GZipStream, BrotliStream, CompressionLevel, HttpClientFactory, IFileProvider, IChangeToken, AddScoped, AddSingleton, AddTransient, ConfigureServices."
---

# Writing Extensions & Compression Code

This skill provides implementation guidance for `Microsoft.Extensions.*` and `System.IO.Compression` libraries, derived from 15,074 maintainer review votes across 2,947 PRs. For full code review, invoke the `@extensions-reviewer` agent as a sub-agent.

---

## DI Lifetime Decision Tree

*(1,772 DI votes + 1,208 weighted | dimensions: D7, D6)*

When registering a service, choose the lifetime based on these criteria:

```
Is the service stateless or immutable after construction?
├─ Yes → Singleton (TryAddSingleton)
│   └─ Does it hold IDisposable resources?
│       ├─ Yes → Singleton, but verify the container disposes it at shutdown
│       └─ No → Singleton is safe
└─ No (mutable state)
    ├─ Is the state scoped to a logical operation (request, unit of work)?
    │   └─ Yes → Scoped (TryAddScoped)
    │       └─ NEVER inject into a Singleton — captive dependency!
    └─ Is a fresh instance needed every time?
        └─ Yes → Transient (TryAddTransient)
            └─ Avoid for IDisposable types — container tracks them until scope disposal
```

**Key rules:**
- Always use `TryAdd{Lifetime}` for default registrations so users can override.
- Never inject `IServiceProvider` as a service locator — inject the specific dependency.
- Decorator patterns (same interface for inner and outer) must guard against infinite recursion.
- Service registration order matters — later registrations override earlier ones unless `TryAdd` is used.

### Example: Registering a default service

```csharp
public static IServiceCollection AddMyFeature(this IServiceCollection services)
{
    services.TryAddSingleton<IMyService, DefaultMyService>();
    services.TryAddTransient<IMyFactory, DefaultMyFactory>();
    return services;
}
```

### Example: Avoiding captive dependencies

```csharp
// WRONG — scoped service captured by singleton
services.AddSingleton<MySingleton>();  // injects IScopedDep → captive!

// CORRECT — use IServiceScopeFactory to create scopes on demand
public class MySingleton(IServiceScopeFactory scopeFactory)
{
    public async Task DoWorkAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dep = scope.ServiceProvider.GetRequiredService<IScopedDep>();
        // use dep within scope lifetime
    }
}
```

---

## Configuration Binding Patterns

*(3,285 votes, #1 area | dimensions: D8, D13)*

### Decision tree: Binding approach

```
Is the app trimmed or AOT-published?
├─ Yes → Use source-generated configuration binding
│   └─ Verify parity with runtime binder for all types
└─ No
    └─ Use runtime binder: services.Configure<TOptions>(config.GetSection("Key"))
```

**Key rules:**
- Configuration key lookups are **case-insensitive**. All switch/dictionary comparisons must use `StringComparison.OrdinalIgnoreCase`.
- The default binder **suppresses binding errors** and silently skips unrecognized properties. Document this if your code depends on strict binding.
- Source-generated binding **must** produce identical behavior to the runtime binder for all supported types.
- Type conversion failures during binding must produce clear error messages identifying the key and expected type.
- `ChangeToken.OnChange` registrations must be disposed when the owning component is disposed to prevent memory leaks.

### Example: Binding with validation

```csharp
services.AddOptions<MyOptions>()
    .Bind(configuration.GetSection("MyOptions"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Example: Case-insensitive key lookup

```csharp
// WRONG — case-sensitive comparison
if (key == "ConnectionString") { ... }

// CORRECT — matches configuration key semantics
if (string.Equals(key, "ConnectionString", StringComparison.OrdinalIgnoreCase)) { ... }
```

---

## Options Validation Patterns

*(1,274 votes | dimensions: D8, D13)*

- Call `ValidateOnStart()` to fail fast instead of discovering invalid configuration at first request.
- Each `ValidateOnStart()` call must not register duplicate validators — check that the builder doesn't accumulate.
- Use `IValidateOptions<T>` for complex cross-property validation that data annotations cannot express.
- Validation source generator output must match the behavior of runtime validation for all supported attributes.
- Named options must flow the name parameter correctly through the entire pipeline; null names resolve to `Options.DefaultName`.
- `IOptionsSnapshot<T>` is scoped — never inject it into singleton services (captive dependency).

### Example: Custom validator

```csharp
public class MyOptionsValidator : IValidateOptions<MyOptions>
{
    public ValidateOptionsResult Validate(string? name, MyOptions options)
    {
        if (options.MaxRetries < 0)
            return ValidateOptionsResult.Fail("MaxRetries must be non-negative.");

        return ValidateOptionsResult.Success;
    }
}
```

---

## Logging Provider Patterns

*(2,723 votes, #2 area | dimensions: D16, D13, D9)*

### Decision tree: Logging API choice

```
Is this a high-frequency log site (called per-request or per-operation)?
├─ Yes → Use [LoggerMessage] source generator for zero-alloc logging
└─ No → ILogger.Log{Level}("message {Param}", value) is acceptable
```

**Key rules:**
- `[LoggerMessage]`-attributed methods **must** be `static partial` — non-static causes confusing source generator errors.
- Use structured log message templates (`{Name}`) — never embed `$""` string interpolation, which bypasses structured logging.
- **Never log sensitive data** (credentials, tokens, PII) at any level, including Debug/Trace.
- Log exceptions via the `ILogger` overload that accepts `Exception` as a parameter.
- Source-generated logging must produce identical behavior to the runtime path — test both paths.
- Abstractions (`ILogger`, `ILoggerFactory`) belong in `*.Abstractions`; providers in implementation packages.

### Example: High-performance logging

```csharp
public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Retry attempt {Attempt} for {OperationName}")]
    public static partial void RetryAttempt(ILogger logger, int attempt, string operationName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Operation {OperationName} failed")]
    public static partial void OperationFailed(ILogger logger, Exception exception, string operationName);
}
```

---

## Caching Implementation Guidance

*(1,603 votes | dimensions: D20, D5, D6)*

**Key rules:**
- Cache keys must incorporate **all** inputs that affect the result, including format versions and serialization options.
- Key generation must be deterministic and collision-free; cache miss must be distinguishable from cached null.
- Mitigate stampede (thundering herd): when a key expires, only one caller should recompute while others wait.
- Eviction must consider TTL, priority, estimated size, and memory pressure — not just LRU.
- Closures for cache factory methods must not allocate on every access — use static lambdas or cached delegates.
- Distributed cache serialization must handle large objects efficiently — consider compression and streaming for values over 100KB.
- `MemoryCache` and `HybridCache` are used concurrently — entry creation and eviction callbacks may execute on different threads.

### Example: Stampede-safe caching with HybridCache

```csharp
public async Task<MyData> GetDataAsync(string key, CancellationToken ct)
{
    return await hybridCache.GetOrCreateAsync(
        key,
        static async (state, ct) => await state.source.FetchAsync(state.key, ct),
        (source: _dataSource, key),
        cancellationToken: ct);
}
```

---

## Compression Implementation Guidance

*(1,776 votes | dimensions: D12, D5, D19)*

**Key rules:**
- ZIP64 extensions are mandatory for files over 4GB — extra field sizes, offsets, and headers must use 64-bit fields.
- Compression level enum values must map to the native library's values (zlib: BestCompression=9, DefaultCompression=-1).
- Decompression must handle concatenated payloads and partial reads — do not assume a single contiguous stream.
- Provide configurable maximum decompressed size limits to prevent zip-bomb attacks.
- New format support (e.g., zstd in ZIP) requires a feature switch for trim/AOT and explicit opt-in.
- Archive extraction must validate entry paths to prevent path traversal attacks.
- Use `ArrayPool<byte>` for compression buffers; avoid 100KB+ fixed allocations per operation.
- Native library updates (brotli, zlib, zstd) must be tracked; use `LibraryImport` for new P/Invoke with SafeHandle.

### Example: Proper async compression

```csharp
// WRONG — performs compression synchronously before first await
public async Task CompressAsync(Stream input, Stream output, CancellationToken ct)
{
    var data = input.ReadAllBytes();  // sync work before await!
    await output.WriteAsync(Compress(data), ct);
}

// CORRECT — async from the start
public async Task CompressAsync(Stream input, Stream output, CancellationToken ct)
{
    await using var compressor = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true);
    await input.CopyToAsync(compressor, ct);
}
```

### Example: Archive extraction with platform metadata

```csharp
// Preserve Unix file permissions when extracting on Unix
if (!OperatingSystem.IsWindows() && entry.ExternalAttributes != 0)
{
    var unixPermissions = (entry.ExternalAttributes >> 16) & 0x1FF;
    if (unixPermissions != 0)
    {
        File.SetUnixFileMode(destinationPath, (UnixFileMode)unixPermissions);
    }
}
```

---

## Host & Service Lifecycle

*(1,629 votes | dimensions: D15, D6)*

**Key rules:**
- `BackgroundService.ExecuteTask` may be null if the derived class did not call `base.StartAsync` — never assume it is set.
- `Host.StopAsync` must not throw when the cancellation token is canceled — it must allow hosted services to complete shutdown.
- `BackgroundService.ExecuteAsync` exceptions must be observed and logged. Unobserved task exceptions silently crash the host.
- Services must not start accepting work until health checks confirm readiness.
- Shutdown signal handlers must work correctly on all platforms (Windows, Linux, macOS).
- Two hosts in the same process must not interfere via static state — scope static registrations to the host instance.

### Example: Safe BackgroundService

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    try
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessWorkAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
        // Graceful shutdown — expected
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Background processing failed");
        throw; // Let the host observe the failure
    }
}
```

---

## Trim & AOT Safety

*(dimensions: D14, D13)*

- Annotate reflection-using APIs with `[DynamicallyAccessedMembers]`.
- Provide feature switches so the linker can trim optional functionality.
- Verify with `PublishAot=true` that no new IL2xxx warnings are introduced.
- Source-generated alternatives must exist for all reflection-based patterns.

---

## Testing Guidance

*(dimensions: D10, D11)*

- Every bug fix needs a regression test; every feature needs happy-path and edge-case tests.
- **Interop tests for compression** must use files created by external tools — not just round-trip with the same implementation.
- Tests depending on timing must use generous timeouts (3+ minutes for hosted service lifecycle in stress pipelines).
- Tests must not leak global state (environment variables, static fields, singleton registrations).
- Test both source-generated and runtime code paths for configuration binding, logging, and options validation to verify parity.
- Dispose behavior must be explicitly tested — verify resources are released and post-disposal operations throw `ObjectDisposedException`.
- Platform-specific tests must use ConditionalFact/ConditionalTheory with appropriate skip conditions.

---

## Cross-Cutting Reminders

- **Sync/async parity**: Share non-trivial logic via common helpers. Do not duplicate implementations.
- **Abstractions vs implementations**: Interfaces and base classes go in `*.Abstractions`. Implementations go in the concrete package.
- **ConfigureAwait(false)**: Use on all awaited calls in library code.
- **TryAdd over Add**: For default service registrations.
- **Case-insensitive keys**: All configuration key comparisons use `OrdinalIgnoreCase`.
- **Backward compatibility**: Behavioral changes are breaking changes even when signatures are unchanged.

For comprehensive code review, invoke `@extensions-reviewer` which applies all 20 review dimensions with full CHECK coverage.