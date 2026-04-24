---
applyTo: "src/libraries/Microsoft.Extensions.Hosting*/**"
---

# Microsoft.Extensions.Hosting — Folder-Specific Guidance

## Host & Service Lifecycle (D15)

- `Host.StopAsync` must not throw when the cancellation token is canceled — it must allow hosted services and host lifetime to complete their shutdown logic
- Shutdown signal handlers (`UseConsoleLifetime`, SIGTERM) must correctly dispose registrations and propagate to all hosted services on all platforms
- If an app exposes readiness health checks, it should avoid listening for or accepting work until its own readiness conditions are met
- `BackgroundService.ExecuteTask` may be null if the derived class did not call `base.StartAsync` — never assume it is set
- `BackgroundService.ExecuteAsync` exceptions must be observed and logged — unobserved task exceptions silently crash the host
- Graceful shutdown must handle `OperationCanceledException` from the stopping token without logging it as an error

## Hosted Service Ordering

- Hosted services start in registration order and stop in reverse order — document dependencies between services
- Do not assume ordering across `IHostedService` implementations unless explicitly registered in sequence
- Consider `ValidateOnStart` to catch configuration errors during startup instead of at first request

## Thread Safety & Static State (D6)

- Two hosts running in the same process must not interfere via static state
- Static event handlers and registrations must be scoped to the host instance, not the process

## Cross-Platform Correctness (D19)

- Shutdown signal handling (SIGTERM, SIGINT, Ctrl+C) must work correctly on Windows, Linux, and macOS
- Platform-specific behavior differences must be abstracted behind PAL layers or guarded with runtime checks

## Error Handling (D9)

- Exceptions from hosted services must be properly observed — unhandled exceptions in `ExecuteAsync` must not silently terminate the host

## Test Reliability (D11)

- Hosted service lifecycle tests must use generous timeouts (3+ minutes in stress/JIT stress pipelines)
- Avoid tight timing assertions — prefer event-based synchronization over `Task.Delay`-based waits
- Tests must not leak global state (environment variables, static fields) across test runs
