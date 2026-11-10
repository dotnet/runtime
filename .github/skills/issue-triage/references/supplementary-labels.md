# Supplementary Label Recommendations

When completing a triage report, recommend supplementary labels that should be
applied in addition to the primary type label (`bug`, `api-suggestion`,
`enhancement`) and the `area-*` label. These labels help maintainers filter,
prioritize, and route issues.

See [area-label-heuristics.md](area-label-heuristics.md) for `area-*` label
guidance.

## Tenet Labels

Tenet labels identify cross-cutting quality attributes. An issue can have
multiple tenet labels.

| Label | When to recommend | Examples |
|-------|-------------------|----------|
| `tenet-performance` | Issue involves measurable performance (throughput, latency, memory, allocations). Applies to both regressions and improvement requests. | "Make JSON deserialization faster", "Memory leak in HttpClient", autofiler regression reports |
| `tenet-performance-benchmarks` | Issue was filed by the `performanceautofiler[bot]` from the benchmark infrastructure. | Automated regression issues with baseline/compare commits and regression tables |
| `tenet-compatibility` | Issue reports a breaking change, behavioral incompatibility with a previous .NET version, or .NET Framework migration difficulty. | "API behaves differently in .NET 10 vs .NET 9", "breaking change in serialization behavior" |
| `tenet-reliability` | Issue involves crashes, hangs, resource leaks, race conditions, or failures under stress/load -- problems with stability rather than correctness of output. | "Segfault under high load", "deadlock in async code path", "memory leak under stress" |
| `tenet-build-performance` | Issue impacts build time -- official builds, developer inner loop, or CI. | "Source generator slows build by 10s", "incremental build regression" |
| `tenet-acquisition` | Issue affects the experience of acquiring, installing, or setting up .NET. | "Confusing SDK install experience", "global.json resolution issues" |

## Runtime and Technology Labels

These labels identify which runtime or technology subsystem is affected.

| Label | When to recommend | Examples |
|-------|-------------------|----------|
| `runtime-coreclr` | Issue is specific to the CoreCLR runtime (JIT, GC, type system, interop). Does not apply to libraries that run on both runtimes. | JIT codegen bug, CoreCLR-specific GC behavior, type loader issue |
| `runtime-mono` | Issue is specific to the Mono runtime. | Mono interpreter bug, Mono AOT issue, Blazor/WASM runtime behavior |
| `source-generator` | Issue involves a source generator (System.Text.Json, Logging, Configuration, RegexGenerator, etc.). | "JSON source generator fails with init-only properties", "Regex source gen performance" |
| `linkable-framework` | Issue relates to trimming, linking, or NativeAOT compatibility of framework code. | "Type X is not trim-compatible", "linker warning in library Y" |
| `size-reduction` | Issue impacts final application size, primarily for size-sensitive workloads (mobile, WASM, embedded). | "Unnecessary IL in trimmed app", "reduce NativeAOT binary size" |
| `runtime-async` | Issue relates to the runtime-level async infrastructure (async state machines, runtime async feature). | "RuntimeAsync support for NativeAOT", async method overhead |
| `PGO` | Issue relates to Profile-Guided Optimization. | "PGO doesn't optimize this pattern", dynamic PGO regression |
| `EventPipe` | Issue relates to the EventPipe diagnostics infrastructure. | EventPipe data corruption, EventPipe performance |

## Qualifier Labels

Qualifier labels add context that affects prioritization or routing.

| Label | When to recommend | Examples |
|-------|-------------------|----------|
| `regression-from-last-release` | Issue is a confirmed or claimed regression from the most recent stable release. Applies to both functional bugs and performance regressions. | "This worked in .NET 9 but fails in .NET 10" |
| `breaking-change` | Issue describes or proposes a change that would break existing API contracts or observable behavior. | Behavioral change that could break consumers, removed API, changed default |
| `code-analyzer` | Issue proposes a new Roslyn analyzer. These are reviewed as API proposals since analyzers effectively define new rules for the ecosystem. | "Warn when discarding Task return value", "detect non-cancelable Task.Delay" |
| `code-fixer` | Issue proposes a Roslyn code fixer (often paired with `code-analyzer`). | "Auto-fix: convert ComputeHash to static HashData" |
| `optimization` | Issue specifically requests a JIT, GC, or low-level runtime optimization (as opposed to library-level performance improvements). | "RyuJIT should inline this pattern", "GC compaction improvement" |
| `source-build` | Issue relates to building .NET from source. | Source-build failures, missing source-build patches |
| `packaging` | Issue relates to NuGet packaging, shipping, or package content. | "Wrong DLL in NuGet package", "missing ref assembly" |
| `design-discussion` | Issue requires ongoing design discussion before implementation can proceed -- no consensus on approach yet. | API shape debates, architectural trade-off discussions |
| `binaryformatter-migration` | Issue relates to the removal of BinaryFormatter and migration away from it. | "Need migration path for BinaryFormatter usage in X" |

## Workflow Labels

These labels track issue lifecycle and triage state.

| Label | When to recommend | Examples |
|-------|-------------------|----------|
| `needs-author-action` | Issue requires more information or action from the original author before it can proceed. | Missing repro, unclear description, asked for .NET version info |
| `needs-further-triage` | Issue has been initially triaged but needs deeper consideration or reconsideration by the area owner. | Complex issue needing domain expert input |
| `needs-area-label` | Issue is missing an `area-*` label and needs one assigned. | New issue with no area label |
| `untriaged` | Issue has not yet been triaged by the area owner. | Newly filed issue |
| `blocked` | Issue or PR is blocked on something -- see comments for details. | Waiting on a dependency, blocked by design decision |

## Test-Related Labels

These labels apply when the issue is about test infrastructure rather than
product code.

| Label | When to recommend | Examples |
|-------|-------------------|----------|
| `test-enhancement` | Issue requests improvements to test source code (better coverage, better assertions, new test scenarios). | "Add tests for edge case X", "improve test reliability for flaky test Y" |
| `test-bug` | Issue is a bug in test code rather than product code. | "Test asserts wrong expected value", "test has race condition" |
| `increase-code-coverage` | Issue specifically tracks adding test coverage for an under-tested component. | "System.Foo has only 40% code coverage" |
| `disabled-test` | The issue tracks a test that has been disabled in source code. | Issue linked from a `[ActiveIssue]` attribute in test code |

## Meta Labels

| Label | When to recommend | Examples |
|-------|-------------------|----------|
| `tracking` | Issue is a meta-issue that tracks completion of multiple sub-issues. | "Umbrella: improve System.Text.Json performance" with linked sub-issues |
| `tracking-external-issue` | Issue is caused by an external dependency (OS, third-party library) and cannot be directly fixed in dotnet/runtime. | "Blocked on kernel bug", "waiting for OpenSSL fix" |
| `investigate` | Issue needs investigation before it can be classified or acted on -- root cause is unclear. | Unclear crash report, ambiguous behavior that could be by-design or a bug |
