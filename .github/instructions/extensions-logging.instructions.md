---
applyTo: "src/libraries/Microsoft.Extensions.Logging*/**"
---

# Microsoft.Extensions.Logging — Folder-Specific Guidance

## LoggerMessage Source Generator (D16, D13)

- `[LoggerMessage]`-attributed methods must be `static partial` — non-static methods cause confusing source generator errors
- Structured log messages must use template placeholders (`{Name}`) — never embed string interpolation, which bypasses structured logging
- Log exceptions via the `ILogger` overload that accepts `Exception` as a parameter — do not embed `exception.ToString()` in the template
- Source-generated logging must produce identical behavior to the runtime logging path — test both paths for parity
- Generated code must compile without warnings, especially nullable warnings

## Log Levels & Security

- Never log sensitive data (credentials, tokens, PII, request/response bodies) at any level, including Debug/Trace
- `IsEnabled` checks should guard expensive log message construction when not using source-generated methods

## Error Handling & Diagnostics (D9)

- Exceptions from inner operations must be properly wrapped or propagated — do not swallow silently without logging

## Logging Provider Configuration

- Console formatter behavior and scope inclusion must follow established defaults
- Changes to provider configuration must not break existing consumer expectations for log output format
- Logging scope semantics must propagate correctly across async boundaries

## Abstractions vs Implementation (D17)

- `ILogger`, `ILoggerFactory`, `ILoggerProvider`, and logging attributes belong in `Microsoft.Extensions.Logging.Abstractions`
- Concrete providers (Console, Debug, EventSource) belong in their respective implementation packages
- Do not introduce implementation dependencies in the abstractions package

## Performance (D5)

- Hot logging paths (per-request, per-operation) must use `[LoggerMessage]` source generation for zero-allocation logging
- Avoid closures that capture state on frequently called log paths — use static lambdas with explicit state
- String manipulation on log paths must use Span-based APIs to avoid substring allocations
