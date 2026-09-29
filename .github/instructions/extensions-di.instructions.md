---
applyTo: "src/libraries/Microsoft.Extensions.DependencyInjection*/**"
---

# Microsoft.Extensions.DependencyInjection — Folder-Specific Guidance

## Service Lifetime Correctness (D7)

- Use `TryAdd{Lifetime}` instead of `Add{Lifetime}` for default registrations to avoid overriding user-configured services
- Scoped services must never be injected into singleton services (captive dependency) — the scoped service would live for the application lifetime
- Transient IDisposable services are tracked by the container until scope disposal — prefer scoped lifetime for disposable services
- Service registration order matters — later registrations of the same type override earlier ones unless `TryAdd` is used
- Decorator patterns where inner and outer service share the same interface must guard against infinite recursion during resolution
- Do not inject `IServiceProvider` broadly as a service locator — inject the specific service needed

## Service Resolution

- `ActivatorUtilities.CreateInstance` bypasses the container registration or factory for the type being created, but it still resolves constructor dependencies through the provided `IServiceProvider`, which can invoke factories for those dependencies
- Singleton dependencies resolved lazily at first request must not cause response-time spikes — consider `ValidateOnStart`
- Factory delegates (`Func<IServiceProvider, T>`) should follow established parameter ordering conventions

## Abstractions Package (D17)

- `IServiceCollection`, `IServiceProvider`, and related interfaces belong in `Microsoft.Extensions.DependencyInjection.Abstractions`
- The concrete `ServiceProvider` and resolution engine belong in the implementation package
- Moving types between abstractions and implementation is a breaking change
- Package references must target correct and aligned versions across the dependency graph

## Thread Safety (D6)

- Singleton services are accessed concurrently by default — all singleton implementations must be thread-safe

## Trim & AOT (D14)

- Annotate service resolution that uses reflection with `[DynamicallyAccessedMembers]`
- Ensure the DI container works correctly under NativeAOT — test with `PublishAot=true`
- No new IL2xxx trim warnings without explicit justification

## Test Coverage (D10)

- Every bug fix includes a regression test; every new feature has happy-path and edge/error tests
- Dispose behavior must be tested — verify resources are released and post-disposal operations throw `ObjectDisposedException`
- Test captive dependency detection and resolution order across different registration patterns

