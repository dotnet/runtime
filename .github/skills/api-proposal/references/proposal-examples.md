# API Proposal Examples

This document contains curated examples of successful API proposals to guide the drafting of new proposals. Study the structure, not just the content.

## PriorityQueue (#43957) — Large Proposal

> Source: https://github.com/dotnet/runtime/issues/43957

**Scale**: Major new collection type (`PriorityQueue<TElement, TPriority>`).

**What made it succeed**:
- Backed by empirical research: surveyed .NET codebases for usage patterns, ran benchmarks across prototypes. Key finding: "90% of use cases do not require priority updates" and "implementations without update support are 2-3x faster."
- 8 explicit design decisions with rationale (e.g., "does not implement `IEnumerable` because elements cannot be efficiently enumerated in priority order")
- Open questions listed with tentative answers (e.g., "Should we use `KeyValuePair` instead of tuples? — We will use tuple types.")
- Link to a working prototype repo
- Implementation checklist (product code, benchmarks, property-based tests, API docs)
- Clean declaration format for the full API surface — no diff markers, no implementation code

**Key lesson**: This proposal succeeded where the original PriorityQueue proposal (#14032) had stalled for years. The difference was doing the research first — empirical data (benchmarks, codebase surveys) resolved the design tensions that had blocked progress. The prototype was the evidence.

---

## Annotated Summaries

### PriorityQueue.DequeueEnqueue (#75070) — Small Proposal

> Source: https://github.com/dotnet/runtime/issues/75070

**Scale**: Single method addition to an existing type.

**What made it succeed**:
- Concise motivation: "extract-then-insert operations are generally more efficient than sequential extract/insert"
- Referenced prior art: Python's `heapq.heapreplace`
- Showed a concrete use case (linked list merge with priority queue)
- API surface was a single method with a clear `diff` block
- Risk section was brief but specific: "must be correctly optimized to outperform sequential Dequeue/Enqueue"

**Key lesson**: A small proposal can be very short. The motivation was one paragraph. The usage example was one code block. This was sufficient because the concept (optimized pop-push) is well-understood.

---

### snake_case Naming Policies (#782) — Medium Proposal

> Source: https://github.com/dotnet/runtime/issues/782

**Scale**: Adding 4 static properties and 4 enum members across 2 types.

**What made it succeed**:
- Clean `diff` format showing the additions alongside the existing `CamelCase` property
- Referenced Newtonsoft.Json behavior ("same behavior as Newtonsoft.Json")
- Pointed to GitHub's API as a concrete, widely-known use case for snake_case
- Scope was complete — covered both lower and upper variants for snake_case AND kebab-case

**Key lesson**: The proposal succeeded because the reviewer could immediately understand it from the diff alone. The motivation was brief because snake_case support is a well-understood need.

---

### Convert.ToHexString Lowercase (#60393) — Small Proposal

> Source: https://github.com/dotnet/runtime/issues/60393

**Scale**: Adding a parameter to existing overloads.

**What made it succeed**:
- Pointed to internal code that already supported the feature (`HexConverter.ToString` accepts casing)
- Motivation was brief: "in some scenarios lowercase is required"
- The implementation was trivial because the capability already existed internally

**Key lesson**: When the internal infrastructure already supports a feature, the proposal can be minimal. The key evidence was "the internal class already supports this."

---

### Async ZipFile APIs (#1541) — Large Pattern-Extension Proposal

> Source: https://github.com/dotnet/runtime/issues/1541

**Scale**: Large API surface (30+ async counterparts across 3 types and 1 extension class), but following a well-understood pattern.

**What made it succeed**:
- Motivation was one sentence: "All ZipFile APIs are currently synchronous. This means manipulations to zip files will always block a thread."
- Clean `diff` format showing each async method alongside its sync counterpart — the reviewer could immediately see the 1:1 mapping
- Explicitly addressed scope decisions: noted that `CreateEntry` had no async work inside, so `CreateEntryAsync` was proposed as optional
- Multiple usage examples covering different scenarios (read, update, create from directory)
- Implementation plan with a phased checklist

**Key lesson**: A large API surface that follows a well-understood pattern (async counterparts for sync APIs) needs minimal motivation — the case is self-evident. The proposal's value was in being thorough about scope (which methods genuinely benefit from async) and providing clean, reviewable diffs. Depth of text was minimal despite the large surface area.

---

### Options ValidateOnStart (#36391) — Medium Behavioral Proposal

> Source: https://github.com/dotnet/runtime/issues/36391

**Scale**: Single extension method, but with significant behavioral implications for application startup.

**What made it succeed**:
- Clear problem statement: "we want to get immediate feedback on validation problems — exceptions thrown on app startup rather than later"
- Terse API surface — just one extension method — but with detailed explanation of how it interacts with the existing `IOptions`/`IOptionsSnapshot`/`IOptionsMonitor` ecosystem
- Usage example was 3 lines of fluent builder code that immediately communicated the intent
- Explicitly scoped: "these APIs don't trigger for IOptionsSnapshot and IOptionsMonitor, where values may get recomputed on every request"

**Key lesson**: Even a single method can require behavioral context when it changes when things happen (startup vs. lazy). The proposal was terse on API surface but precise on behavioral semantics — which aspects of the existing system it interacts with and which it doesn't.

---

### JsonSerializerOptions.RespectNullableAnnotations (#74385) — Medium Proposal

> Source: https://github.com/dotnet/runtime/issues/74385

**Scale**: Adding a boolean property with significant behavioral implications.

**What made it succeed**:
- Strong community demand (83 reactions) established the motivation
- Detailed behavior specification covering edge cases
- Addressed the "what about source generators?" question proactively
- Backward compatibility was carefully designed (opt-in behavior)

**Key lesson**: Even a single boolean property can require extensive proposal text when the behavioral implications are significant. The depth was proportional to the novelty of the concept, not the API surface size.
