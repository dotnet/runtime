# Enhancement Triage

Guidance for triaging issues labeled `enhancement` in dotnet/runtime.
Referenced from the main [SKILL.md](../SKILL.md) during Step 5.

An enhancement is a request to improve existing behavior **without adding new
public API surface**. If the issue proposes new public API, it should be
labeled `api-suggestion` and triaged with the
[API proposal guide](api-proposal-triage.md) instead.

## Classify the Enhancement

Enhancements in dotnet/runtime fall into several subcategories. Classify the
issue first, because each subcategory has different evaluation criteria.

| Subcategory | Indicators | Examples |
|-------------|-----------|----------|
| **Performance improvement** | Requests faster execution, lower allocations, reduced memory, better throughput; may cite benchmarks or profiling data | SIMD for JSON parsing, io_uring for sockets, compact string representation |
| **Behavioral improvement** | Requests better defaults, improved error messages, more consistent behavior, or smarter handling of edge cases -- all without new API | `HttpClient` throwing `TaskCanceledException` instead of `TimeoutException` on timeout, `BackgroundService` blocking host startup, readable stack traces |
| **Infrastructure / tooling** | Improvements to internal tooling, source generators, diagnostics, cDAC, build system, or test infrastructure | Source generator fixes, cDAC API support, code coverage improvements |
| **Platform support** | Requests support for a new OS, architecture, or platform-specific feature | FreeBSD support, Linux kernel TLS offload, ARM64 optimizations |
| **Porting / parity** | Requests porting a .NET Framework component or achieving feature parity across platforms | Port System.Xaml, DirectoryServices on Linux, EventLog on .NET Core |

If an issue spans multiple subcategories, pick the primary one and note the
overlap.

## Feasibility Analysis

For each enhancement, conduct a planning-level feasibility analysis. The goal
is to determine whether the enhancement is architecturally viable and worth
pursuing -- **not** to prototype or implement anything.

### Questions to investigate

1. **Scope** -- How much of the codebase would this touch? Is it isolated to
   one library/component, or does it cut across multiple areas?
2. **Backward compatibility** -- Could this change break existing consumers?
   Even behavioral improvements without API changes can break code that depends
   on current behavior (intentionally or accidentally).
3. **Architecture fit** -- Does this align with the current architecture of the
   affected component? Or would it require significant restructuring?
4. **Maintenance burden** -- What is the ongoing cost of maintaining this
   change? Platform-specific features and porting efforts carry perpetual
   maintenance obligations.
5. **Dependencies** -- Does this depend on external factors (OS features, third-
   party libraries, specification stability)?

### Subcategory-specific evaluation

**Performance improvements:**

- Is the performance claim substantiated? Look for benchmark data, profiling
  results, or a reproducible scenario. Unsubstantiated "X is slow" claims
  should request data (NEEDS INFO).
- What is the expected magnitude of the improvement? A 2× throughput gain on
  a hot path is very different from a 5% reduction in a cold path.
- Are there trade-offs? Performance improvements often add code complexity,
  increase binary size (e.g., SIMD code paths), or sacrifice readability.
- Does the improvement benefit real-world workloads, or only synthetic
  microbenchmarks?

**Behavioral improvements:**

- Is the current behavior documented or contractual? Changing undocumented
  behavior is lower risk than changing behavior described in API docs.
- Could any consumer depend on the current behavior? Search for patterns
  that rely on the existing behavior using grep.app or GitHub code search.
- Is this a consistency fix (aligning with how similar APIs already behave)?
  Consistency fixes have stronger justification than arbitrary changes.

**Infrastructure / tooling:**

- Is this primarily of internal value (maintainer productivity) or does it
  have user-facing impact (e.g., source generator improvements)?
- What is the risk of introducing regressions in build/test infrastructure?
- Is there a clear owner for this area?

**Platform support:**

- How large is the user base for the target platform?
- Is there an existing community contribution or RFC?
- What is the ongoing maintenance commitment? Platform-specific code paths
  require continuous CI investment and expertise.

**Porting / parity:**

- Is the component being requested actively maintained in .NET Framework?
- Are there licensing or IP considerations?
- Is there a community-maintained alternative that could be promoted instead?
- What is the realistic scope -- is this a bounded port or an open-ended
  compatibility commitment?

## Trade-off Assessment

Summarize the trade-offs in the triage report:

| Dimension | Assessment |
|-----------|-----------|
| **Value** | What does this gain? (performance, usability, correctness, platform reach) |
| **Cost** | What does this cost? (code complexity, maintenance burden, binary size, risk) |
| **Scope** | How much work is involved? (S/M/L/XL estimate) |
| **Risk** | What could go wrong? (backward compat, subtle behavioral changes, platform-specific failures) |
| **Alternatives** | Are there simpler ways to achieve the same goal? (configuration switch, NuGet package, documentation) |

## Enhancement-Specific Recommendation Criteria

### KEEP

- The enhancement addresses a real, demonstrable problem (not speculative).
- Feasibility analysis shows the change is architecturally viable.
- The value clearly outweighs the cost and risk.
- For performance improvements: data substantiates the claim (benchmarks,
  profiling, or a clear algorithmic argument).
- For behavioral improvements: the current behavior is inconsistent,
  surprising, or clearly suboptimal, and the change is unlikely to break
  existing consumers.
- For platform support / porting: there is demonstrated demand and a
  realistic maintenance plan.

### CLOSE

- The enhancement is better served by a community NuGet package or
  out-of-tree solution.
- The maintenance burden is disproportionate to the value.
- The requested behavior change would break existing consumers with no
  viable mitigation path.
- For porting requests: the component is deprecated or superseded in
  .NET Framework itself.
- For performance improvements: the expected gain is negligible or the
  proposal optimizes a path that is not performance-sensitive in practice.

### NEEDS INFO

- **Performance improvement without data** -- Ask for benchmark results,
  profiling output, or a reproducible scenario that demonstrates the problem.
- **Behavioral change with unclear impact** -- Ask whether any code depends
  on the current behavior; request a concrete scenario where the current
  behavior causes problems.
- **Vague scope** -- The enhancement is reasonable in principle but too vague
  to assess feasibility. Ask for a more specific description of the desired
  change.
- **Platform support without demand signal** -- Ask about the target audience
  size and whether there are community contributors willing to help maintain
  the platform-specific code.
