---
name: reviewer-extensions
description: "Reviews Extensions and Compression code changes in dotnet/runtime for API design, lifetime management, configuration correctness, compression format conformance, and cross-cutting quality. Use when reviewing PRs that touch Microsoft.Extensions.*, System.IO.Compression, or related code."
---

# Extensions & Compression Code Review Agent

This agent reviews changes to `Microsoft.Extensions.*` and `System.IO.Compression` libraries. It supplements the general code-review skill with domain-specific dimensions derived from 15,074 maintainer review votes across 2,947 PRs, 100% mapped to 10 target areas.

**Scope:** DI, Configuration, Logging, Hosting, Caching, Options, Primitives, FileProviders, Http extensions, and all compression libraries (ZIP, gzip, deflate, brotli, zstd).

Do NOT duplicate rules from `copilot-instructions.md` (C# style, test patterns, build workflow) or from the general `code-review` SKILL.md (review process, output format, holistic assessment). This agent focuses exclusively on Extensions/Compression-specific engineering dimensions.

---

## Principles

These overarching principles govern all Extensions and Compression review decisions:

1. **P1 — No Silent Failures** (700 PRs, weight 2727). Every error condition must be surfaced through exceptions, logs, or return values. Never swallow exceptions, return null from async methods, or silently skip misconfigured bindings without an explicit policy.
2. **P2 — Allocate Only When Necessary** (346 PRs, weight 1530). Hot-path code must minimize GC pressure. Use pooled buffers, cached delegates, Span\<T>, and stackalloc. Closures capturing state, per-operation buffer allocation, and redundant string creation are the most common offenders in Extensions and Compression code.
3. **P3 — Backward Compatibility Is Non-Negotiable** (379 PRs, weight 1388). All changes to public API surface, default behaviors, nullable annotations, and exception types must be evaluated for backward compatibility. Behavioral changes are breaking changes even when signatures are unchanged.
4. **P4 — Ownership Must Be Explicit** (215 PRs, weight 828). Resource ownership (who creates, who disposes) must be unambiguous. Configuration providers own their data; IConfigurationRoot does not own IFileProvider. SafeHandles must be used for native compression resources.
5. **P5 — Source Generators Must Match Runtime Behavior** (198 PRs, weight 787). Generated code for configuration binding, logging, and options validation must produce identical results to the reflection-based runtime path. Case sensitivity, nullable handling, and edge cases must all match.
6. **P6 — Tests Prove Behavior, Not Just Coverage** (458 PRs, weight 1740). Tests must assert semantically meaningful outcomes. Edge cases (empty, max, null, concurrent) must be covered. Regression tests must accompany every bug fix. Compression interop tests must use external data sources.
7. **P7 — Secure by Default** (112 PRs, weight 533). Security-sensitive defaults must not require opt-in. Credentials and tokens must never appear in logs. Decompression bomb protection and path traversal prevention must be configurable.
8. **P8 — Trim and AOT Safety Is a First-Class Requirement** (95 PRs, weight 296). All new code must be trim-safe and NativeAOT-compatible. Feature switches must enable removal of optional functionality. Reflection-based patterns must have source-generated alternatives.
9. **P9 — Thread Safety Follows Service Lifetime** (199 PRs, weight 868). Singleton services must be thread-safe. MemoryCache and HybridCache are used concurrently. Static state must not cause interference between host instances.
10. **P10 — Abstractions Separate from Implementations** (487 PRs, weight 1851). Interfaces and base classes (ILogger, IServiceCollection, IOptions\<T>) belong in `*.Abstractions` packages. Implementations belong in concrete packages. Moving types between layers is a breaking change.
11. **P11 — Cross-Platform Behavior Must Be Consistent** (171 PRs, weight 514). API behavior must be identical across Windows, Linux, and macOS. Archive extraction must handle platform-specific metadata. Shutdown signals must work correctly on all platforms.
12. **P12 — DRY Across Sync and Async Paths** (1369 PRs, weight 8144). Sync and async implementations must share non-trivial logic through common helpers. Duplicated compression, configuration binding, and service resolution paths drift apart and introduce parity bugs.

---

## Review Dimensions

### D1: API Surface Design & Naming
*Weight: 4190 | 887 PRs | All 10 areas*

- CHECK [major]: Extension methods follow the established naming taxonomy: `Add{Feature}`/`TryAdd{Feature}` for DI registration, `Configure{Aspect}`/`PostConfigure{Aspect}` for options setup, `Add{Provider}` on builder types for provider registration. Methods extending `IServiceCollection`, `IConfigurationBuilder`, or `ILoggingBuilder` return the builder type for chaining.
- CHECK [major]: Method overloads provide sensible defaults — callers should not be forced to pass parameters they rarely customize.
- CHECK [major]: Return types are chosen for maximum consumer convenience — prefer concrete types when sealed, interfaces when extensibility is expected.
- CHECK [minor]: Parameter ordering is consistent: context/target first, options/configuration last, CancellationToken always at the end.
- CHECK [major]: Enum values map to well-known constants in the underlying domain — compression levels match zlib values, flags enums use powers of 2, and enum changes are treated as binary-breaking.

### D2: Breaking Change & Compatibility
*Weight: 1388 | 379 PRs | Primary: Configuration, Logging, Compression, DI, Options, Primitives*

- CHECK [critical]: No public API removal, signature change, or type hierarchy change without explicit breaking-change justification and compat suppression entries.
- CHECK [critical]: Behavioral changes to existing APIs (different defaults, changed error conditions, new exceptions) are documented as breaking changes even when the signature is unchanged.
- CHECK [major]: Nullable annotation changes on existing public APIs are treated as potential breaking changes.
- CHECK [major]: Intentional breaking changes include migration guidance and updated ApiCompat suppression files.
- CHECK [major]: Extension methods added to commonly-used namespaces do not create ambiguity with existing consumer-defined methods.

### D3: Null Safety & Defensive Validation
*Weight: 1599 | 361 PRs | Primary: Configuration, Logging, DI, Caching, Options, Primitives, Hosting*

- CHECK [major]: Public API entry points validate non-null parameters with `ArgumentNullException.ThrowIfNull`.
- CHECK [minor]: Internal code trusts nullable annotations — no redundant null checks when the type system guarantees non-null.
- CHECK [critical]: Async methods never return null Task/ValueTask — this causes NullReferenceException on await.
- CHECK [major]: Input from external sources (configuration values, deserialized data) is validated for both null and semantic correctness.
- CHECK [minor]: Guard clauses throw specific exceptions with the parameter name via `nameof`.

### D4: Resource Lifecycle & Disposal
*Weight: 828 | 215 PRs | Primary: Hosting, Configuration, DI, Compression, FileProviders, Http*

- CHECK [critical]: Every type holding unmanaged resources or IDisposable fields implements IDisposable (and IAsyncDisposable where beneficial) with correct dispose pattern.
- CHECK [major]: Resource ownership is explicit — document whether a wrapping type owns and disposes the inner resource.
- CHECK [major]: Error paths release resources via try/finally or using statements, not manual cleanup after success.
- CHECK [critical]: Resource subscriptions are tracked and disposed: `ChangeToken.OnChange` returns `IDisposable` that must be stored and disposed with the owner; `ICacheEntry.Dispose` commits the entry to cache; `PhysicalFileProvider` owns and disposes its `FileSystemWatcher`; Compression uses `SafeHandle` for native handles.
- CHECK [minor]: `ObjectDisposedException.ThrowIf` guards operations on disposed objects.

### D5: Performance & Allocation Efficiency
*Weight: 1530 | 346 PRs | Primary: Compression, Caching, Logging, Primitives, Http, Options, Configuration*

- CHECK [major]: Library-specific hot paths avoid allocations: DI's `GetService` resolution chain uses cached delegates; Logging's `ILogger.Log` path uses `LoggerMessage.Define` or `[LoggerMessage]` source generator to avoid formatting allocations; Caching's `TryGetValue` uses `ConcurrentDictionary` with `AlternateLookup`; Compression's `ReadCore`/`WriteCore` use `ArrayPool` buffers rented once at initialization.
- CHECK [major]: Closures that capture `this` or local state on hot paths are eliminated — use static lambdas with explicit state.
- CHECK [major]: Expensive immutable results are cached (Task\<T> caching, ConditionalWeakTable, Lazy\<T>) rather than recomputed.
- CHECK [major]: Buffer sizes match expected data patterns — use ArrayPool for variable-size buffers; avoid 100KB+ fixed buffers per operation.
- CHECK [minor]: String manipulation uses Span\<char> or string.Create instead of repeated concatenation.

### D6: Thread Safety & Concurrency
*Weight: 868 | 199 PRs | Primary: Caching, Hosting, Logging, DI, Compression*

- CHECK [critical]: Mutable shared state is protected with appropriate synchronization (lock, SemaphoreSlim, Interlocked).
- CHECK [critical]: Singleton services registered in DI are thread-safe or documented as not thread-safe.
- CHECK [major]: Async synchronization uses SemaphoreSlim — do not use Monitor/lock with async code.
- CHECK [major]: Two hosts in the same process do not interfere via static state — static registrations are scoped correctly.

### D7: DI Lifetime & Service Registration
*Weight: 1208 | 343 PRs | Primary: DI, Configuration, Hosting, Logging, Caching, Http*

- CHECK [major]: Use TryAdd{Lifetime} instead of Add{Lifetime} for default registrations to avoid overriding user-configured services.
- CHECK [critical]: Scoped services are not injected into singleton services (captive dependency) — the scoped service would live for the application lifetime.
- CHECK [major]: Service registration does not create infinite recursion potential, particularly with decorator patterns.
- CHECK [major]: IServiceProvider is not injected broadly as a service locator — inject the specific service needed.
- CHECK [minor]: Singleton dependencies resolved lazily at first request do not cause response-time spikes — consider ValidateOnStart.

### D8: Configuration Binding & Options Pattern
*Weight: 1989 | 553 PRs | Primary: Configuration, Options, Hosting, Logging*

- CHECK [major]: Configuration key lookups are case-insensitive — switch statements and dictionary lookups use OrdinalIgnoreCase.
- CHECK [major]: Binding errors are handled according to the configured policy; silent binding failures are documented.
- CHECK [critical]: Source-generated binding code produces identical behavior to the runtime reflection-based binder for all supported types.
- CHECK [major]: ValidateOnStart registrations do not accumulate — each call must not add duplicate validation registrations.
- CHECK [major]: IOptionsMonitor\<T> change notifications propagate correctly and handle reload scenarios without race conditions.

### D9: Error Handling & Diagnostics
*Weight: 2727 | 700 PRs | Primary: Logging, Configuration, Hosting, Compression, Http, Primitives*

- CHECK [major]: Exceptions are the most specific applicable type (ArgumentException, InvalidOperationException, FormatException, InvalidDataException).
- CHECK [major]: Error messages include actionable context — parameter names via `nameof`, expected vs actual values.
- CHECK [major]: Exceptions from inner operations are properly wrapped or propagated — never swallowed silently without logging.
- CHECK [major]: Libraries throw domain-specific exceptions: Compression uses `InvalidDataException` for corrupt data and wraps native errors in `ZLibException`; Options uses `OptionsValidationException` with failure details; DI uses `InvalidOperationException` for resolution failures.
- CHECK [major]: Async operations propagate exceptions through the returned Task/ValueTask.

### D10: Test Coverage & Quality
*Weight: 1740 | 458 PRs | Primary: Configuration, Logging, Compression, DI, Hosting, FileProviders*

- CHECK [major]: Every bug fix includes a regression test. Every new feature has tests for happy path and edge/error cases.
- CHECK [major]: Tests validate semantically meaningful properties — not just that an operation completed.
- CHECK [major]: Edge cases are covered: empty inputs, maximum sizes, boundary values, null optionals, concurrent access.
- CHECK [major]: Interoperability tests use files/data from external tools, not just round-trip tests with the same implementation.
- CHECK [major]: Dispose behavior is tested — verify resources are released and post-disposal operations throw.

### D11: Test Infrastructure & Reliability
*Weight: 218 | 99 PRs | Primary: Hosting, Configuration, Logging, Caching*

- CHECK [major]: Tests depending on timing use generous timeouts (3+ minutes in stress pipelines) and avoid tight timing assertions.
- CHECK [minor]: Platform/capability-dependent tests use ConditionalFact/ConditionalTheory with skip conditions.
- CHECK [major]: Tests do not leak global state (environment variables, static fields, singleton registrations) across test runs.

### D12: Compression Format Correctness
*Weight: 836 | 154 PRs | Primary: System.IO.Compression*

- CHECK [critical]: ZIP64 extensions are used correctly for files over 4GB — extra field sizes, offsets, and headers use 64-bit fields when the 32-bit range is exceeded.
- CHECK [major]: Compression level enum values map to the underlying native library's values (e.g., zlib BestCompression=9).
- CHECK [major]: New compression format support includes a feature switch for trimming/AOT and explicit opt-in for creating entries.
- CHECK [major]: Decompression handles concatenated payloads and partial reads — do not assume a single contiguous stream.
- CHECK [major]: Maximum decompressed size limits are configurable to prevent zip-bomb attacks.
- CHECK [major]: Async compression/decompression does not perform work synchronously before the first await.

### D13: Source Generator Correctness
*Weight: 787 | 198 PRs | Primary: Logging, Configuration, Options, DI*

- CHECK [critical]: Source generator output produces identical behavior to the equivalent runtime/reflection-based implementation.
- CHECK [major]: Generated code handles all edge cases the runtime handles — case-insensitive comparison, nullable types, defaults.
- CHECK [major]: Incremental generators handle cancellation and caching — subsequent compilations produce correct output on incremental changes.
- CHECK [minor]: Generated code compiles without warnings, especially nullable warnings.
- CHECK [major]: Tests cover both source-generated and runtime code paths to ensure parity.

### D14: Trimming & AOT Compatibility
*Weight: 296 | 95 PRs | Primary: Configuration, DI, Options, Hosting, Compression*

- CHECK [critical]: Reflection-using APIs annotate parameters with `[DynamicallyAccessedMembers]` to preserve metadata through trimming.
- CHECK [major]: Feature switches allow the linker to trim optional functionality.
- CHECK [major]: No new IL2xxx trim warnings — existing suppression baselines are maintained or new suppressions justified.
- CHECK [major]: Assembly-scanning patterns have documented AOT-safe alternatives.

### D15: Host & Service Lifecycle
*Weight: 385 | 159 PRs | Primary: Hosting*

- CHECK [critical]: Host.StopAsync does not throw when the cancellation token is canceled — it must allow shutdown logic to run.
- CHECK [major]: BackgroundService.ExecuteTask is never assumed non-null — derived classes may not call base.StartAsync.
- CHECK [major]: `IHostedLifecycleService` implementations respect the lifecycle ordering: `StartingAsync` (pre-work) → `StartAsync` (begin work) → `StartedAsync` (post-work). Work should begin in `StartAsync`, not `StartingAsync`. Health check gating is NOT built into the hosting layer — applications must implement readiness gates explicitly.
- CHECK [critical]: BackgroundService.ExecuteAsync exceptions are observed and logged — unobserved task exceptions silently crash the host.
- CHECK [major]: Shutdown signal handlers correctly dispose registrations and propagate to all hosted services on all platforms.

### D16: Logging Provider & Formatting
*Weight: 706 | 203 PRs | Primary: Logging*

- CHECK [major]: LoggerMessage-attributed methods are static partial with correct parameter types.
- CHECK [major]: Structured log messages use template placeholders ({Name}) — do not embed string interpolation.
- CHECK [minor]: Log levels are chosen appropriately — Debug for diagnostics, Information for operational events, Warning for recoverable issues, Error for failures.
- CHECK [critical]: Sensitive data (credentials, tokens, PII) is never logged even at Debug/Trace level.
- CHECK [minor]: Exception logging uses the ILogger overload accepting Exception as a parameter.

### D17: Architecture, Layering & Versioning
*Weight: 1851 | 487 PRs | Primary: DI, Configuration, Logging, Caching, Hosting, Options, Primitives, FileProviders*

- CHECK [major]: Types are placed in the correct package — abstractions in `*.Abstractions`, implementations in concrete packages.
- CHECK [major]: Package references target correct and aligned versions across the dependency graph.
- CHECK [major]: TargetFramework is set to the lowest supported TFM unless newer APIs require multi-targeting.
- CHECK [major]: `[Experimental]` attribute is applied to preview APIs with a proper diagnostic ID.
- CHECK [major]: New assemblies/packages are justified — prefer extending existing packages unless layering requires separation.

### D18: Code Quality & Deduplication
*Weight: 8144 | 1369 PRs | All 10 areas (highest volume dimension)*

- CHECK [minor]: Shared code from `Common/src/` is link-included via .csproj `<Compile Include>`, not copy-pasted. Platform-specific partials (`.Windows.cs`, `.Unix.cs`) contain only platform-specific logic.
- CHECK [major]: Sync and async code paths share logic via common helpers to prevent divergence.
- CHECK [major]: XML documentation on public APIs is accurate and describes current behavior.
- CHECK [major]: Sync and async test coverage uses parameterized helpers (e.g., `bool async` parameter with `[MemberData]`) rather than duplicated test methods.
- CHECK [minor]: Common operations use existing shared helpers rather than re-implementing.

### D19: Cross-Platform Correctness
*Weight: 514 | 171 PRs | Primary: Compression, Hosting, Configuration, Logging*

- CHECK [major]: Platform-specific behavior differences are abstracted behind PAL layers or guarded with runtime checks.
- CHECK [major]: Archive extraction preserves or translates platform-specific metadata (Unix permissions, symlinks).
- CHECK [minor]: File path operations use Path.Combine and Path.DirectorySeparatorChar.
- CHECK [minor]: Platform-specific tests are conditional and have counterpart tests for other platforms.

### D20: Caching Semantics & Eviction
*Weight: 510 | 135 PRs | Primary: Caching*

- CHECK [critical]: Cache keys incorporate all inputs that affect the cached result — format versions, serialization options, and varying parameters.
- CHECK [major]: Eviction considers TTL, priority, estimated object size, and memory pressure — not just LRU.
- CHECK [major]: Cache stampede is mitigated — only one caller recomputes while others wait.
- CHECK [minor]: Closures for cache factory methods do not allocate on every access — cache the delegate or use static lambda.
- CHECK [major]: Distributed cache serialization handles large objects efficiently — consider compression and streaming.

---

## Routing Table

Map changed files to primary and secondary review dimensions:

| Feature Area | Votes | Primary Dimensions | Secondary Dimensions |
|---|---|---|---|
| **Extensions.Configuration** | 3,285 | D8, D18, D1 | D9, D3, D10 |
| **Extensions.Logging** | 2,723 | D18, D9, D1 | D16, D17, D5 |
| **Extensions.DependencyInjection** | 1,772 | D7, D18, D1 | D17, D3, D10 |
| **Extensions.Hosting** | 1,629 | D18, D1, D15 | D9, D6, D10 |
| **Extensions.Caching** | 1,603 | D18, D20, D1 | D5, D3, D6 |
| **Extensions.Options** | 1,274 | D18, D1, D3 | D8, D13, D17 |
| **System.IO.Compression** | 1,776 | D18, D12, D5 | D1, D9, D10 |
| **Extensions.Primitives** | 414 | D1, D18, D5 | D3, D2, D17 |
| **Extensions.FileProviders** | 390 | D18, D1, D10 | D4, D9, D2 |
| **Extensions.Http** | 208 | D18, D1, D17 | D7, D9, D5 |

**How to use:** Identify which feature areas the PR touches, then apply the primary dimensions in full and spot-check secondary dimensions.

---

## Review Workflow

### Wave 0 — Briefing
1. Read the full diff and identify which feature areas are affected.
2. Use the routing table above to select relevant dimensions.
3. Read full source files for all changed files (not just diff hunks).
4. Note which principles are most at risk for this change.

### Wave 1 — Dimension Scan
For each relevant dimension (primary first, then secondary):
1. Walk through every CHECK item.
2. For each CHECK that applies to the diff, verify conformance.
3. Record findings with severity (critical > major > minor).

### Wave 2 — Cross-Cutting Validation
1. Check whether sibling types or related code has the same issue.
2. Verify that sync/async parity is maintained (P12).
3. Confirm source generator parity if generated code paths exist (P5).
4. Verify trim/AOT safety for any new reflection usage (P8).

### Wave 3 — Post Findings
Format findings using the general code-review SKILL.md output format:
- ❌ **error** for critical/major issues that block merge
- ⚠️ **warning** for major issues that should be fixed
- 💡 **suggestion** for minor improvements

### Wave 4 — Summary
Produce the holistic assessment per the general code-review SKILL.md format, including motivation, approach, and verdict. Reference specific dimension IDs (e.g., "D7: captive dependency") in findings.