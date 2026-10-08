---
name: extensions-review
description: "Guidance for writing and modifying Microsoft.Extensions.* and System.IO.Compression code in dotnet/runtime. Covers DI lifetime management, configuration binding, options validation, logging provider patterns, caching semantics, compression format compliance, and host lifecycle. For full code review, delegates to the @extensions-reviewer agent. Trigger words: Microsoft.Extensions, IServiceCollection, IConfiguration, ILogger, IHost, IMemoryCache, IOptions, ZipArchive, HttpClientFactory, IFileProvider, IChangeToken."
---

# Writing Extensions & Compression Code

This skill provides implementation guidance for `Microsoft.Extensions.*` and `System.IO.Compression` libraries. For full code review, invoke the `@extensions-reviewer` agent as a sub-agent.

---

## DI Lifetime Decision Tree

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

### Decision tree: Binding approach

```
Is the app trimmed or AOT-published?
├─ Yes → Use source-generated configuration binding
│   └─ Verify parity with runtime binder for all types
└─ No
    └─ Use runtime binder: services.Configure<TOptions>(config.GetSection("Key"))
```

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

### Decision tree: Logging API choice

```
Is this a high-frequency log site (called per-request or per-operation)?
├─ Yes → Use [LoggerMessage] source generator for zero-alloc logging
└─ No → ILogger.Log{Level}("message {Param}", value) is acceptable
```

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

### Example: Stampede-safe caching with HybridCache (available in .NET 9+)

```csharp
public async Task<MyData> GetDataAsync(string key, CancellationToken ct)
{
    return await hybridCache.GetOrCreateAsync(
        key,
        (source: _dataSource, key),
        static async (state, ct) => await state.source.FetchAsync(state.key, ct),
        cancellationToken: ct);
}
```

---

## Compression Implementation Guidance

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

- Annotate reflection-using APIs with `[DynamicallyAccessedMembers]`.
- Provide feature switches so the linker can trim optional functionality.
- Verify with `PublishAot=true` that no new IL2xxx warnings are introduced.
- Source-generated alternatives must exist for all reflection-based patterns.

---

## Testing Guidance

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

For comprehensive code review, invoke `@extensions-reviewer` which applies the full review checklist with complete CHECK coverage.