---
name: code-review
description: Review code changes in dotnet/runtime for correctness, performance, and consistency with project conventions. Use when reviewing PRs or code changes.
---

# dotnet/runtime Code Review

Review code changes against conventions and patterns established by dotnet/runtime maintainers. These rules were extracted from 43,000+ maintainer review comments across 6,600+ PRs and represent the actual standards enforced in practice.

**Reviewer mindset:** Be polite but very skeptical. Your job is to help speed the review process for maintainers, which includes not only finding problems the PR author may have missed but also questioning the value of the PR in its entirety. Treat the PR description and linked issues as claims to verify, not facts to accept. Question the stated direction, probe edge cases, and don't hesitate to flag concerns even when unsure.

## When to Use This Skill

Use this skill when:
- Reviewing a PR or code change in dotnet/runtime
- Checking code for correctness, performance, style, or consistency issues before submitting a PR
- Asked to review, critique, or provide feedback on code changes
- Validating that a change follows dotnet/runtime conventions

## Review Process

### Step 0: Gather Code Context (No PR Narrative Yet)

Before analyzing anything, collect as much relevant **code** context as you can. **Critically, do NOT read the PR description, linked issues, or existing review comments yet.** You must form your own independent assessment of what the code does, why it might be needed, what problems it has, and whether the approach is sound — before being exposed to the author's framing. Reading the author's narrative first anchors your judgment and makes you less likely to find real problems.

1. **Diff and file list**: Fetch the full diff and the list of changed files.
2. **Full source files**: For every changed file, read the **entire source file** (not just the diff hunks). You need the surrounding code to understand invariants, locking protocols, call patterns, and data flow. Diff-only review is the #1 cause of false positives and missed issues.
3. **Consumers and callers**: If the change modifies a public/internal API, a type that others depend on, or a virtual/interface method, search for how consumers use the functionality. Grep for callers, usages, and test sites. Understanding how the code is consumed reveals whether the change could break existing behavior or violate caller assumptions.
4. **Sibling types and related code**: If the change fixes a bug or adds a pattern in one type, check whether sibling types (e.g., other abstraction implementations, other collection types, platform-specific variants) have the same issue or need the same fix. Fetch and read those files too.
5. **Key utility/helper files**: If the diff calls into shared utilities, read those to understand the contracts (thread-safety, idempotency, etc.).
6. **Git history**: Check recent commits to the changed files (`git log --oneline -20 -- <file>`). Look for related recent changes, reverts, or prior attempts to fix the same problem. This reveals whether the area is actively churning, whether a similar fix was tried and reverted, or whether the current change conflicts with recent work.
7. **Detect new public API surface**: Check whether the PR introduces new public API surface. Look for:
   - Changes to `ref/` assembly source files (the strongest signal — these define the public API contract)
   - New `public` members (methods, properties, types, enum values) in `src/` files
   - Note whether new public API was detected. If it was, you **MUST** load and execute the API approval verification procedure during Step 3. Read the file `.github/skills/code-review/api-approval-check.md` (relative to the repository root) and follow its instructions. Do not skip this step — it is blocking.
  
### Step 1: Discover Area-Specific Agents
- Study **review** agents available in  `.github/agents` folder that are capable of reviewing specific areas of the codebase. Their yaml frontmatter description tells when they apply.
- When performing the review, invoke sub-agents to perform those area-specific reviews as subtasks during all subsequent steps, integrating those results.
- Depending on the PR, more subagents might be launched. Launch them in parallel. Always continue regular review described here as well - the subagents are addons, not replacements.

### Step 2: Form an Independent Assessment

Based **only** on the code context gathered above (without the PR description or issue), answer these questions:

1. **What does this change actually do?** Describe the behavioral change in your own words by reading the diff and surrounding code. What was the old behavior? What is the new behavior?
2. **Why might this change be needed?** Infer the motivation from the code itself. What bug, gap, or improvement does it appear to address?
3. **Is this the right approach?** Would a simpler alternative be more consistent with the codebase? Could the goal be achieved with existing functionality? Are there correctness, performance, or safety concerns?
4. **What problems do you see?** Identify bugs, edge cases, missing validation, thread-safety issues, performance regressions, API design problems, test gaps, and anything else that concerns you.

Write down your independent assessment before proceeding. You must produce a holistic assessment (see [Holistic PR Assessment](#holistic-pr-assessment)) at this stage.

### Step 3: Incorporate PR Narrative and Reconcile

Now read the PR description, labels, linked issues (in full), author information, existing review comments, and any related open issues in the same area. Treat all of this as **claims to verify**, not facts to accept.

1. **PR metadata**: Fetch the PR description, labels, linked issues, and author. Read linked issues in full — they often contain the repro, root cause analysis, and constraints the fix must satisfy.
2. **Related issues**: Search for other open issues in the same area (same labels, same component). This can reveal known problems the PR should also address, or constraints the author may not be aware of.
3. **Existing review comments**: Check if there are already review comments on the PR to avoid duplicating feedback.
4. **Reconcile your assessment with the author's claims.** Where your independent reading of the code disagrees with the PR description or issue, investigate further — but do not simply defer to the author's framing. If the PR claims a bug fix, a performance improvement, or a behavioral correction, verify those claims against the code and any provided evidence. If your independent assessment found problems the PR narrative doesn't acknowledge, those problems are more likely to be real, not less.
5. **Update your holistic assessment** if the additional context reveals information that genuinely changes your evaluation (e.g., a linked issue proves the bug is real, or an existing review comment already identified the same concern). But do not soften findings just because the PR description sounds reasonable.
6. **API Approval Verification.** If Step 0 detected new public API surface, you **MUST** now load the file `.github/skills/code-review/api-approval-check.md` (relative to the repository root) and execute the full procedure described there. Use the `view` tool, `cat`, or equivalent to read the file contents into your context, then follow every step. This is a **blocking** gate — if any check in that procedure fails, the review verdict must be ❌ Reject or ❌ Needs Changes regardless of other findings. Do not proceed without completing this step when new public API is detected. **If the file cannot be loaded for any reason**, report ❌ error — "Unable to load API approval verification procedure; cannot verify new public API surface" — and set the verdict to ❌ Needs Changes.

### Step 4: Detailed Analysis

1. **Focus on what matters.** Prioritize bugs, performance regressions, safety issues, race conditions, resource management problems, incorrect assumptions about data or state, and API design problems. Do not comment on trivial style issues unless they violate an explicit rule below.
2. **Consider collateral damage.** For every changed code path, actively brainstorm: what other scenarios, callers, or inputs flow through this code? Could any of them break or behave differently after this change? If you identify any plausible risk — even one you can't fully confirm — surface it so the author can evaluate. Do not dismiss behavioral changes because you believe the fix justifies them. The tradeoff is the author's decision — your job is to make it visible.
3. **Be specific and actionable.** Every comment should tell the author exactly what to change and why. Reference the relevant convention. Include evidence of how you verified the issue is real, e.g., "looked at all callers and none of them validate this parameter".
4. **Flag severity clearly:**
   - ❌ **error** — Must fix before merge. Bugs, security issues, API violations, test gaps for behavior changes.
   - ⚠️ **warning** — Should fix. Performance issues, missing validation, inconsistency with established patterns.
   - 💡 **suggestion** — Consider changing. Style improvements, minor readability wins, optional optimizations.
5. **Don't pile on.** If the same issue appears many times, flag it once on the primary file with a note listing all affected files. Do not leave separate comments for each occurrence.
6. **Respect existing style.** When modifying existing files, the file's current style takes precedence over general guidelines.
7. **Don't flag what CI catches.** Do not flag issues that a linter, typechecker, compiler, analyzer, or CI build step would catch, e.g., missing usings, unsupported syntax, formatting. Assume CI will run separately.
8. **Avoid false positives.** Before flagging any issue:
   - **Verify the concern actually applies** given the full context, not just the diff. Open the surrounding code to check. Confirm the issue isn't already handled by a caller, callee, or wrapper layer before claiming something is missing.
   - **Skip theoretical concerns with negligible real-world probability.** "Could happen" is not the same as "will happen."
   - **If you're unsure, either investigate further until you're confident, or surface it explicitly as a low-confidence question rather than a firm claim.** Do not speculate about issues you have no concrete basis for. Every comment should be worth the reader's time.
   - **Trust the author's context.** The author knows their codebase. If a pattern seems odd but is consistent with the repo, assume it's intentional.
   - **Never assert that something "does not exist," "is deprecated," or "is unavailable" based on training data alone.** Your knowledge has a cutoff date. When uncertain, ask rather than assert.
9. **Ensure code suggestions are valid.** Any code you suggest must be syntactically correct and complete. Ensure any suggestion would result in working code.
10. **Label in-scope vs. follow-up.** Distinguish between issues the PR should fix and out-of-scope improvements. Be explicit when a suggestion is a follow-up rather than a blocker.

## Multi-Model Review

When the environment supports launching sub-agents with different models (e.g., the `task` tool with a `model` parameter), run the review in parallel across multiple model families to get diverse perspectives. Different models catch different classes of issues. If the environment does not support this, proceed with a single-model review.

**How to execute (when supported):**
1. Inspect the available model list and select models from 2-3 distinct model families, up to 3 sub-agent models total. If fewer than 2 eligible families are available, use what is available. **Model selection rules:**
   - Pick only from models explicitly listed as available in the environment. Do not guess or assume model names.
   - From each selected family, pick the model with the highest capability tier (prefer "premium" or "standard" over "fast/cheap").
   - Never pick models labeled "mini", "fast", or "cheap" for code review.
   - Do not select the same model that is already running the primary review (i.e., your own model). The goal is diverse perspectives from different model families.
   - **Do not use `gpt-5.4`** — it has known reliability issues causing sub-agent timeouts in >90% of affected runs. For the OpenAI/GPT family, prefer `gpt-5.3-codex` if it is explicitly listed as available; otherwise, fall back to the highest-version non-blocked GPT model that satisfies the other rules here.
   - If multiple standard-tier models exist in the same family (excluding blocked models above), pick the one with the highest version number. Prefer "-codex" variants over general-purpose for code review tasks.
2. Launch a sub-agent for each selected model in parallel, giving each the same review prompt: the PR diff, the review rules from this skill, and instructions to produce findings in the severity format defined above.
3. Wait for all agents to complete, then synthesize: deduplicate findings that appear across models, elevate issues flagged by multiple models (higher confidence), and include unique findings from individual models that meet the confidence bar. **Timeout handling:** If a sub-agent has not completed after 10 minutes and you have results from other agents, proceed with the results you have. Do not block the review indefinitely waiting for a single slow model. Note in the output which models contributed.
4. Present a single unified review to the user, noting when an issue was flagged by multiple models. **After posting the review, immediately exit.** Do not wait for any remaining sub-agents. Do not attempt retries if the comment was posted successfully. The review is complete once the post operation succeeds or returns a comment URL.


---

## Review Output Format

When presenting the final review (whether as a PR comment or as output to the user), use the following structure. This ensures consistency across reviews and makes the output easy to scan.

> 📝 **AI-generated content disclosure:** When posting review content to GitHub (PR review comments, PR comments) under a user's credentials — i.e., the account is **not** a dedicated "copilot" or "bot" account/app — you **MUST** include a concise, visible note (e.g. a `> [!NOTE]` alert) indicating the content was AI/Copilot-generated. Skip this if the user explicitly asks you to omit it.

### Structure

```
## 🤖 Copilot Code Review — PR #<number>

### Holistic Assessment

**Motivation**: <1-2 sentences on whether the PR is justified and the problem is real>

**Approach**: <1-2 sentences on whether the fix/change takes the right approach>

**Summary**: <✅ LGTM / ⚠️ Needs Human Review / ⚠️ Needs Changes / ❌ Reject>. <2-3 sentence summary of the overall verdict and key points. If "Needs Human Review," explicitly state which findings you are uncertain about and what a human reviewer should focus on.>

---

### Detailed Findings

#### ✅/⚠️/❌ <Category Name> — <Brief description>

<Explanation with specifics. Reference code, line numbers, interleavings, etc.>

(Repeat for each finding category. Group related findings under a single heading.)
```

### Guidelines

- **Holistic Assessment** comes first and covers Motivation, Approach, and Summary.
- **Detailed Findings** uses emoji-prefixed category headers:
  - ✅ for things that are correct / look good (use to confirm important aspects were verified)
  - ⚠️ for warnings or impactful suggestions (should fix, or follow-up)
  - ❌ for errors (must fix before merge)
  - 💡 for minor suggestions or observations (nice-to-have)
- **Cross-cutting analysis** should be included when relevant: check whether related code (sibling types, callers, other platforms) is affected by the same issue or needs a similar fix.
- **Test quality** should be assessed as its own finding when tests are part of the PR.
- **Summary** gives a clear verdict: LGTM (no blocking issues — use only when confident), Needs Human Review (code may be correct but you have unresolved concerns or uncertainty that require human judgment), Needs Changes (with blocking issues listed), or Reject (explaining why this should be closed outright). **Never give a blanket LGTM when you are unsure.** When in doubt, use "Needs Human Review" and explain what a human should focus on.
- Keep the review concise but thorough. Every claim should be backed by evidence from the code.

### Verdict Consistency Rules

The summary verdict **must** be consistent with the findings in the body. Follow these rules:

1. **The verdict must reflect your most severe finding.** If you have any ⚠️ findings, the verdict cannot be "LGTM." Use "Needs Human Review" or "Needs Changes" instead. Only use "LGTM" when all findings are ✅ or 💡 and you are confident the change is correct and complete.

2. **When uncertain, always escalate to human review.** If you are unsure whether a concern is valid, whether the approach is sufficient, or whether you have enough context to judge, the verdict must be "Needs Human Review" — not LGTM. Your job is to surface concerns for human judgment, not to give approval when uncertain. A false LGTM is far worse than an unnecessary escalation.

3. **Separate code correctness from approach completeness.** A change can be correct code that is an incomplete approach. If you believe the code is right for what it does but the approach is insufficient (e.g., treats symptoms without investigating root cause, silently masks errors that should be diagnosed, fixes one instance but not others), the verdict must reflect the gap — do not let "the code itself looks fine" collapse into LGTM.

4. **Classify each ⚠️ and ❌ finding as merge-blocking or advisory.** Before writing your summary, decide for each finding: "Would I be comfortable if this merged as-is?" If any answer is "no," the verdict must be "Needs Changes." If any answer is "I'm not sure," the verdict must be "Needs Human Review."

5. **Devil's advocate check before finalizing.** Re-read all your ⚠️ findings. For each one, ask: does this represent an unresolved concern about the approach, scope, or risk of masking deeper issues? If so, the verdict must reflect that tension. Do not default to optimism because the diff is small or the code is obviously correct at a syntactic level.

---

## Holistic PR Assessment

Before reviewing individual lines of code, evaluate the PR as a whole. Consider whether the change is justified, whether it takes the right approach, and whether it will be a net positive for the codebase.

### Motivation & Justification

- **Every PR must articulate what problem it solves and why.** Don't accept vague or absent motivation. Ask "What's the rationale?" and block progress until the contributor provides a clear answer.
- **Challenge every addition with "Do we need this?"** New code, APIs, abstractions, and flags must justify their existence. If an addition can be avoided without sacrificing correctness or meaningful capability, it should be.
- **Demand real-world use cases and customer scenarios.** Hypothetical benefits are insufficient motivation for expanding API surface area or adding features. Require evidence that real users need this.

### Evidence & Data

- **Require measurable performance data before accepting optimization PRs.** Demand BenchmarkDotNet results or equivalent proof — never accept performance claims at face value.
- **Distinguish real performance wins from micro-benchmark noise.** Trivial benchmarks with predictable inputs overstate gains from jump tables, branch elimination, and similar tricks. Require evidence from realistic, varied inputs.
- **Investigate and explain regressions before merging.** Even if a PR shows a net improvement, regressions in specific scenarios must be understood and explicitly addressed — not hand-waved.

### Approach & Alternatives

- **Check whether the PR solves the right problem at the right layer.** Look for whether it addresses root cause or applies a band-aid. Prefer fixing the actual source of an issue over adding workarounds to production code.
- **When a PR takes a fundamentally wrong approach, redirect early.** Don't iterate on implementation details of a flawed design. Push back on the overall direction before the contributor invests more time.
- **Ask "Why not just X?" — always prefer the simplest solution.** When a PR uses a complex approach, challenge it with the simplest alternative that could work. The burden of proof is on the complex solution.

### Cost-Benefit & Complexity

- **Explicitly weigh whether the change is a net positive.** A performance trade-off that shifts costs around is not automatically beneficial. Demand clarity that the change is a win in the typical configuration, not just in a narrow scenario.
- **Reject overengineering — complexity is a first-class cost.** Unnecessary abstraction, extra indirections, and elaborate solutions for marginal gains are actively rejected.
- **Every addition creates a maintenance obligation.** Long-term maintenance cost outweighs short-term convenience. Code that is hard to maintain, increases surface area, or creates technical debt needs stronger justification.

### Scope & Focus

- **Require large or mixed PRs to be split into focused changes.** Each PR should address one concern. Mixed concerns make review harder and increase regression risk.
- **Defer tangential improvements to follow-up PRs.** Police scope creep by asking contributors to separate concerns. Even good ideas should wait if they're not part of the PR's core purpose.

### Risk & Compatibility

- **Flag breaking changes and require formal process.** Any behavioral change that could affect downstream consumers needs documentation, API review, and explicit approval — even when the change improves the codebase internally.
- **Assess regression risk proportional to the change's blast radius.** High-risk changes to stable code need proportionally higher value and more thorough validation.

### Codebase Fit & History

- **Ensure new code matches existing patterns and conventions.** Deviations from established patterns create confusion and inconsistency. If a rename or restructuring is warranted, do it uniformly in a dedicated PR — not piecemeal.
- **Check whether a similar approach has been tried and rejected before.** If a prior attempt didn't work, require a clear explanation of what's different this time.

## Correctness & Safety

### Error Handling & Assertions

- **Use `Debug.Assert` for internal invariants, not exceptions.** For internal-only callers, assert assumptions rather than throwing `ArgumentException`. Prefer `Debug.Assert(value != null)` over the null-forgiving operator (`!`).
- **Use `throw` for reachable error paths, `UnreachableException` for exhaustive switches.** When a code path might be hit at runtime, throw an exception rather than asserting. Use `throw new UnreachableException()` for default cases in exhaustive switches. Use `PlatformNotSupportedException` (not `NotSupportedException`) for platform gaps. In native code, use `_ASSERTE(!"message")`.
- **Include actionable details in exception messages.** Use `nameof` for parameter names. Include the unsupported type or unexpected value. Never throw empty exceptions.
- **Initialize output parameters in all code paths.** When a method has `out` parameters or pointer outputs (`bytesWritten`, `numLocals`), ensure they are initialized to a defined value in all error paths.
- **Handle OOM with exceptions or fail-fast, never asserts.** Use `ThrowOutOfMemory` or `EEPOLICY_HANDLE_FATAL_ERROR`, not asserts. In interpreter loops, use `nothrow new` and check for null.
- **Use `ThrowIf` helpers over manual checks.** Use `ArgumentOutOfRangeException.ThrowIfNegative`, `ObjectDisposedException.ThrowIf`, etc. instead of manual if-then-throw patterns.
- **Challenge exception swallowing that masks unexpected errors.** When a PR adds try/catch blocks that silently discard exceptions (`catch { continue; }`, `catch { return null; }`), question whether the exception represents a truly expected, recoverable condition or an unexpected error signaling a deeper problem (race conditions, memory corruption, build environment issues). Silently catching exceptions that "shouldn't happen" hides root causes and makes debugging harder. The default disposition should be to let unexpected exceptions propagate or fail fast so the real issue gets investigated.

### Thread Safety

- **Use `Volatile` or `Interlocked` for cross-thread field access.** Fields written on one thread and read on another must use `Volatile<T>`, `Volatile.Read/Write`, or `Interlocked`. The `??=` operator is not thread-safe. `Nullable<T>` is not safe for caching (two-field struct tears). Do not use shared mutable arrays without synchronization.
- **Use `TickCount64` for timeout calculations.** Use `Environment.TickCount64` (long) instead of `Environment.TickCount` (int) to avoid integer overflow.

### Security

- **Guard integer arithmetic against overflow.** Guard size computations involving multiplication (e.g., `newCapacity * sizeof(T)`) against integer overflow. Use patterns correct by construction.
- **Clean sensitive cryptographic data after use.** Always clear key material with `CryptographicOperations.ZeroMemory`. When using `PinAndClear` but copying to another buffer, clear the original too. Use non-short-circuit operators (`|`) in verification code to prevent timing leaks.
- **Don't proactively send credentials without opt-in.** Never send authentication credentials (especially Basic auth) before receiving a challenge.
- **Limit `stackalloc` to ~1KB and validate size.** Don't stackalloc based on user-controlled or large input sizes. Move stackalloc to just before usage, not before early returns.

### Correctness Patterns

- **Fix root cause, not symptoms or workarounds.** Investigate and fix the root cause rather than adding workarounds or suppressing warnings. Revert broken commits before layering fixes.
- **Prefer safe code over unsafe micro-optimizations.** Do not introduce `Unsafe.As`, `Unsafe.AsRef`, or raw pointers without demonstrable performance need. Prefer Span-based APIs. If performance is the issue, prefer fixing the JIT.
- **Use `Unsafe.BitCast` for same-size type punning.** Prefer `Unsafe.BitCast<TFrom, TTo>` over `Unsafe.As<TFrom, TTo>` for type punning between value types of the same size.
- **Delete dead code and unnecessary wrappers.** Remove dead code, unnecessary wrappers, obsolete fields, and unused variables when encountered or when the only caller changes.
- **Handle `SafeHandle.IsInvalid` before `Dispose`.** Check `IsInvalid` (not null) on returned SafeHandles. Get the exception before calling `Dispose`, since Dispose might clear the error state.
- **Seal classes when `Equals` uses exact type matching.** If a class implements `Equals` with `GetType()` comparison, seal the class to prevent subtle inheritance bugs.
- **Use `Environment.ProcessPath` and `AppContext.BaseDirectory`.** Use these instead of `Process.GetCurrentProcess().MainModule?.FileName` and `Assembly.Location` for NativeAOT/single-file compatibility.
- **File name casing must match csproj references exactly.** Linux is case-sensitive. New source files must be listed in the `.csproj` if other files in that folder are explicitly listed.
- **Prefer correct-by-construction designs.** Prefer designs that are correct by construction (e.g., scanning IL) over manually maintained parallel data structures. A missed optimization is better than silent bad codegen.
- **Allocate on the correct loader allocator for collectibility.** When allocating runtime data structures for generic instantiations, use the correct loader allocator accounting for collectibility of type arguments.
- **Backport targeted fixes, not refactorings.** When backporting to servicing branches, create small targeted fixes. Backporting large refactorings introduces unnecessary risk.

### JIT-Specific Correctness

- **JIT lowering must not double-lower nodes.** Never call `LowerNode` on an already-lowered node. Return newly created nodes for the caller to lower. Constant folding belongs in import/morph, not lowering.
- **Mark collectible ALC test methods `NoInlining`.** Methods that touch collectible assembly load contexts must be `[MethodImpl(MethodImplOptions.NoInlining)]` to prevent the JIT from keeping references alive.
---

## Performance & Allocations

### Measurement & Evidence

- **Performance changes require benchmark evidence.** Include BenchmarkDotNet or EgorBot numbers before merging. Validate with real-world scenarios, not just microbenchmarks.
- **Justify binary size increases with real-world measurements.** Changes that increase binary size require measured wall-clock improvements on real-world apps, not just instruction counts.
- **Avoid premature optimization with object pools and caches.** Do not introduce global caches or object pools without evidence they are needed. Prefer making the underlying operation faster.

### Allocation Avoidance

- **Avoid closures and allocations in hot paths.** When a lambda captures locals creating a closure, consider using a static delegate with a state parameter (value tuple). Avoid string concatenation; use span-based operations.
- **Pre-allocate collections when size is known.** Pass capacity to `Dictionary`, `HashSet`, `List` constructors when the expected count is available.
- **Structs in dictionaries need `IEquatable<T>` and `GetHashCode`.** Without these, the runtime falls back to boxing allocations for equality comparison.
- **Avoid Pinned Object Heap for non-permanent objects.** POH is never compacted and effectively gen2. Only use for objects surviving as long as the process.
- **Suppress `ExecutionContext` flow for infrastructure timers.** When allocating `Timer` or similar background infrastructure, suppress EC flow to avoid capturing unrelated `AsyncLocal`s that leak memory.

### Code Structure for Performance

- **Place cheap checks before expensive operations.** Order conditionals so cheapest/most-common checks come first. Move expensive work after early-exit checks.
- **Allocate resources lazily where possible.** Allocate expensive resources on first use, not during initialization. Avoid forcing type initialization during startup.
- **Extract throw helpers into `[DoesNotReturn]` methods.** Move throwing logic from error paths into separate static local functions or helper methods to allow the JIT to inline the success path.
- **Avoid O(n²) patterns in collections and hot paths.** Watch for linear scans inside loops, repeated `RemoveAt` in loops. Use `RemoveAll`, single-pass restructuring, or appropriate data structures.
- **Cache repeated accessor calls in locals.** Store the result of repeated property/getter calls in a local variable.
- **Separate hot data from rarely-used data in runtime structures.** Keep frequently accessed data inline; move rarely-used data (GCInfo, DebugInfo) to separate structures.
- **Compute constant data at compile time, not execution time.** In interpreter and similar hot paths, pre-compute metadata lookups and type checks during the compilation phase.
- **Consider scalability, not just throughput.** Evaluate whether data structures, caches, and locking strategies will hold up at high cardinality or under concurrent load. Watch for unbounded collection growth, lock contention that worsens with core count, and O(1) assumptions that break at scale.

### Specific API Choices

- **Use `AppContext.TryGetSwitch` with a static readonly property.** Cache AppContext switches in `static bool Prop { get; } = AppContext.TryGetSwitch(...)` so the JIT can dead-code-eliminate unreachable paths.
- **Do not cache `typeof` expressions in .NET Core.** `typeof(...)` is JITed into a constant; caching it is a de-optimization. Similarly, don't store `ArrayPool.Shared` in variables—it breaks devirtualization.
- **Use `CollectionsMarshal` for large value-type dictionary lookups.** Use `GetValueRefOrAddDefault` or `GetValueRefOrNullRef` to avoid copying large structs. Use `ValueListBuilder` on hot paths.
- **Use `sizeof` instead of `Marshal.SizeOf` for blittable structs.** `sizeof` is more correct and significantly faster when no marshalling is involved.
- **Use the idiomatic `(uint)index >= (uint)length` bounds check.** The JIT recognizes this pattern and optimizes it. Slice spans before iterating to avoid per-element bounds checks.
- **Source generators must be properly incremental.** Do not store Roslyn symbols (`ISymbol`, `Compilation`) in incremental pipeline steps. Output must be deterministic with Ordinal-sorted lists.
- **Avoid LINQ and records in low-level compiler codebases.** In CG2/ILC and AOT tools, use direct loops instead of LINQ and readonly structs instead of records. Use concrete types over interfaces in private code.
- **Use `ValueListBuilder` for dynamic array building in BCL.** Use `ValueListBuilder<T>` (with pooling) or `ArrayBuilder<T>`. Use stackalloc for small sizes, array pool when too large.
---

## API Design & Contracts

- **New public APIs require approved proposals before PR submission.** All new API surface must go through API review. PRs adding unapproved APIs will be closed. The implementation must match exactly what was approved. When new public API surface is detected, the API approval verification procedure (`.github/skills/code-review/api-approval-check.md`) is executed to enforce this rule.
- **Use `internal` for new APIs pending API review.** If the API is needed immediately for implementation, mark it `internal` and file a review request separately.
- **Parameter names must match between ref and src.** Renaming a public API parameter (including case changes) is a breaking change affecting named arguments and late-bound scenarios.
- **Align exception types and validation order across platforms.** Validate arguments first (`ArgumentNullException`, then `ArgumentException`), then `PNSE`, then `ObjectDisposedException`, then perform the operation. Throw the same exception types on all platforms.
- **`Try` APIs should return `false` only for the common expected failure.** Throw for everything else (corruption, permissions, invalid arguments). Try methods must always throw on invalid arguments.
- **Don't expose mutable options after construction.** If values are captured at construction time, don't expose a mutable options object. Don't reference private field names or internal types in user-facing error messages.
- **Use `PlatformNotSupportedException` for platform limitations.** When an operation can't complete in the current environment but could on a different platform, throw PNSE. Don't impose artificial limits beyond OS capabilities.
- **.NET APIs should compensate for platform quirks.** Public APIs should work consistently across platforms. When adding overloads, check F# compatibility for implicit conversion ambiguities.
- **Follow the obsoletion process for deprecated APIs.** Pick the next available SYSLIB diagnostic ID, add `[Obsolete]`, and use `[EditorBrowsable(Never)]` with `[OverloadResolutionPriority(-1)]` for overload fixes.
- **New GC-EE interface methods must be appended last.** Always add new methods as the last method on the interface to preserve vtable slot ordering.
- **New virtual methods must work with unoverridden derived types.** The default implementation must behave identically to calling the pre-existing equivalent APIs.
- **Avoid unsigned types for lengths in public APIs.** Prefer `int` or `long` for length parameters. Use named types instead of `ValueTuple` across file boundaries.
- **Start core component changes with an issue.** Changes to host, VM, or JIT should start with a GitHub issue describing the problem and motivation before submitting a PR.
---

## Code Style & Formatting

- **Use well-named constants instead of magic numbers.** No raw hex or decimal constants without explanation. Don't duplicate magic constants across files.
- **Use `var` only when the type is obvious from context.** Use explicit types for casts, method returns, and async infrastructure. Never use `var` for numeric types.
- **Use PascalCase for constants; descriptive names for booleans.** All constant locals and fields use PascalCase (except interop constants matching external names). Boolean fields should be positive and descriptive (`_hasCurrent` not `valid`).
- **Name methods to accurately reflect their behavior.** Update names when behavior changes. `Get*` implies a return value; use `Print*/Display*` for void. `ThrowIf` not `ThrowExceptionIf`.
- **Prefer early return to reduce nesting.** Use early returns for short/error cases to avoid unnecessary nesting. Put the error case first, success return last.
- **Avoid `using static` and `#region` in new code.** `using static` is costly when reading code outside IDEs (e.g., GitHub review). `#region` gets out of date quickly.
- **Place local functions at method end, fields first in types.** Local functions go at the end of the containing method. Fields are the first members declared in a type.
- **Narrow warning suppression to smallest scope.** Avoid file-wide `#pragma` suppressions. Disable only around the specific line that triggers the warning.
- **Use pattern matching and `is`/`or`/`and` patterns.** Prefer `is` patterns and C# pattern matching over manual type checks and comparisons. Use named parameters for boolean arguments.
- **Do not initialize managed fields to default values (CA1805).** The CLR zero-initializes all fields in managed code. Explicit `= false`, `= 0`, `= null` is redundant. (This does not apply to native C/C++ code, where fields and locals must be explicitly initialized.)
- **Sealed classes do not need the full Dispose pattern.** A simple `Dispose()` is sufficient since no derived class can introduce a finalizer.
- **Prefer table-driven approaches over excessive case statements.** For hardware intrinsics and pattern-heavy code, use lookup tables (`AuxiliaryJitType`, `SpecialCodeGen` flags) instead of many explicit case entries.
- **Order struct fields to minimize padding.** In C/C++ struct definitions, order fields by size (pointers first) to reduce padding.
---

## Consistency with Codebase Patterns

### PR Hygiene

- **Keep PRs focused on their stated scope.** No accidental file modifications, no unrelated refactoring, no whitespace noise, no build artifacts. Each PR should serve a single purpose.
- **Do large refactorings and renames in separate PRs.** Separate no-diff refactors from functional changes. Mechanical renames should be separate from logic changes.
- **Merge to main first, then backport to release branches.** Use the `/backport` command. Backports to servicing are limited to security bugs, regressions, and reliability issues.

### Code Reuse & Deduplication

- **Extract duplicated logic into shared helper methods.** Fix improvements inside shared helpers so all callers benefit.
- **Move shared code to shared files, not duplicated across runtimes.** When identical code exists across CoreCLR and NativeAOT, move it to the shared partition (using `#if !MONO` if needed).
- **Use existing APIs instead of creating parallel ones.** Before introducing new types, enums, or helpers, check if existing ones serve the same purpose. Fix existing utilities rather than introducing duplicates.
- **Delete dead code and unused declarations aggressively.** When removing code, also remove helper methods, enum values, function declarations, and resx strings that are no longer used.

### Established Conventions

- **Store error strings in `.resx`, not inline code.** Reference via the `SR` class. When removing code that uses a resx string, delete the unused string entry.
- **Sort lists and entries alphabetically.** Lists of areas, configuration entries, resx entries, entrypoint/export lists, and ref source members should be maintained in alphabetical order.
- **Don't modify auto-generated files or `eng/common` manually.** Change the generator or source definition instead. Files in `eng/common` are synced from dotnet/arcade.
- **Use `DOTNET_` prefix for environment variables, not `COMPlus_`.** New runtime environment variables must use `DOTNET_` exclusively.
- **Match existing style in modified files.** The existing style in a file takes precedence over general guidelines. Do not change existing code for style alone.
- **Prefer `sizeof` over `Unsafe.SizeOf` consistently.** A pass was done to replace all `Unsafe.SizeOf` uses. Do not reintroduce them.

### Runtime-Specific Patterns

- **Consider NativeAOT parity for runtime changes.** When changing CoreCLR behavior, verify whether the same change is needed for NativeAOT.
- **Keep interpreter behavior consistent with the regular JIT.** Follow the same patterns, naming, error codes (`CORJIT_BADCODE`), and macros (`NO_WAY`). Use `FEATURE_INTERPRETER` guards.
- **Source generators: no file locks, diagnostics from analyzers only.** Generators should bypass invalid state gracefully. A separate analyzer should produce diagnostics.
- **Ref assembly conventions.** No `using` directives (fully qualify types), empty method bodies or `throw null`, genapi-style formatting, alphabetical member order. TFM-specific APIs go in separate files.
---

## Testing

- **Always add regression tests for bug fixes and behavior changes.** Prefer adding `[InlineData]` test cases to existing test files rather than creating new ones. Ensure new test files are included in the csproj.
- **Use platform-specific test attributes correctly.** Use `[PlatformSpecific]`, `[ConditionalFact]`, or `[ActiveIssue]` for skip logic rather than runtime if-checks. `ConditionalFact` is required for `SkipTestException` to work.
- **Test edge cases, error paths, and all affected types.** Include empty strings, negative values, boundary conditions, Turkish 'i', surrogate pairs. Test both true and false for boolean options. Choose inputs that can't accidentally pass if output wasn't touched.
- **Test assertions must be specific.** Assert exact expected values (exact `OperationStatus`, exact byte counts), not broad conditions. Ensure tests actually fail when the fix is reverted.
- **Delete flaky and low-value tests rather than patching them.** Do not add tests known to be flaky. If a test relies on fragile runtime details and cannot be made reliable, prefer deletion.
- **Make test data deterministic and culture-independent.** Create `CultureInfo` with explicit format settings. Use `[Theory]` with `[InlineData]` over individual `[Fact]` methods.
- **Use `PLACEHOLDER` for test passwords.** Avoids false positives from credential scanning tools.
- **Use checked builds for CI, lower priority for regression tests.** Use checked (not debug) CoreCLR builds for CI. New JIT regression tests should typically be `CLRTestPriority 1`.
- **Use `RemoteExecutor` for tests with process-wide shared state.** Tests that modify shared state should use `RemoteExecutor` for isolation. Avoid hardcoded paths; use temp files. Do not add heavy dependencies like `Microsoft.CodeAnalysis.CSharp` to test assemblies.
- **Catch only expected exceptions in fuzz tests.** Catching all exceptions masks bugs like undocumented exceptions escaping the API.
- **Use modern xUnit patterns for xUnit-based tests.** In xUnit test projects (for example, most libraries tests), use `Assert.*` instead of the legacy `return 100 == success` pattern, use `[Fact]`/`[Theory]`, prefer `ThrowsAnyAsync<OperationCanceledException>` for cancellation, and name regression test classes after the issue number (e.g., `Runtime_117605`). Legacy non-xUnit tests under `src/tests` may continue to use the existing `return 100` convention.
- **Reduce test output volume.** Avoid megabytes of console output. Use `Thread.Sleep` with fewer iterations instead of busy loops.
- **Follow naming conventions for regression test directories.** In `src/tests/Regressions/coreclr/`, use `GitHub_<issue_number>` for the directory and `test<issue_number>` for the test name.
---

## Documentation & Comments

- **Comments should explain why, not restate code.** Delete comments like `// Get the types` that just duplicate the code in English. Don't include historical context about why code changed.
- **Delete or update obsolete comments when code changes.** Stale comments describing old behavior are worse than no comments.
- **Track deferred work with GitHub issues and searchable TODOs.** Reference a tracking issue in TODO comments with a consistent prefix (e.g., `TODO-Async:`). Remove ancient TODOs that will never be addressed.
- **Don't duplicate comments on interface implementations.** Documentation comments belong on the interface definition. Duplicating leads to divergence.
- **Add XML doc comments on all new public APIs.** These seed the official API documentation on learn.microsoft.com. Properties should start with "Gets the ..." or "Gets or sets the ...". Do not add XML docs to test code.
- **Use SHA-specific or commit-based links in documentation.** Don't use branch-relative links that break when files move.
- **Reference ECMA-335 and spec sources in metadata code.** When parsing signatures and metadata, cite the relevant ECMA-335 section. Cite CAVP/ACVP sources in crypto test vectors.
- **File breaking change documentation for behavioral changes.** Open an issue in dotnet/docs using the template, send notification to the .NET Breaking Change Notification DL. Applies even to prerelease-to-prerelease changes.
- **Use established terminology in user-facing text.** Do not expose internal type names, private field names, or codenames like "Roslyn" in public docs or error messages.
- **Retain copyright headers and license information.** All C# and C++ source files must include the standard license header, including test files. When porting from other projects, retain original copyright and update THIRD-PARTY-NOTICES.TXT.
---

## Platform & Cross-Platform

- **Use `BinaryPrimitives` for endianness-safe reads.** Use `ReadInt32LittleEndian`/`BigEndian` rather than pointer casts. Separate endianness-specific reads from target-endianness reads.
- **Use cross-platform vector APIs over ISA-specific intrinsics.** Prefer `Vector128/256/512.IsHardwareAccelerated` and cross-platform APIs (`.Shuffle`, `.Min`) over `Avx512BW`, `SSE2`. Use `BitOperations` for portable bit manipulation.
- **Use correct platform/feature defines.** Use `TARGET_*`/`HOST_*` defines rather than compiler-provided defines (`__wasm__`). Use `HOST_*` for build machine code, `TARGET_*` for target platform. Use `PORTABILITY_ASSERT` for unimplemented platform code.
---

## Native Code & Interop

### C++ Style

- **Don't use `auto` in the runtime C++ codebase.** Use explicit types. Exception: unspeakable types like lambdas.
- **Use `nullptr`, `void*`, and native C++ types over legacy aliases.** Prefer `nullptr` over `NULL`, `void*` over `LPVOID`. Use `WCHAR` (not `wchar_t`) in Windows host code. Use `.inc` suffix for multiply-included files.
- **Match `#endif` comments to `#ifdef` exactly.** Add comments on `#else`/`#endif` for non-trivial blocks. Consistent brace placement and four-space indentation.
- **Prefer `static_cast` over C-style casts.** C-style casts are more permissive than needed and can silently degrade to `reinterpret_cast`.

### Runtime & VM Patterns

- **Use correct VM contracts and QCall patterns.** QCalls that may throw need `BEGIN_QCALL`/`END_QCALL`. Simple QCalls use `QCALL_CONTRACT_NO_GC_TRANSITION`. All VM methods need `STANDARD_VM_CONTRACT` or `WRAPPER_NO_CONTRACT`.
- **Keep GC protection correct around managed references.** Ensure all GC references are `GCPROTECT`-ed before GC-triggering calls. After GC-triggering calls, use `ObjectFromHandle(handle)` for a fresh reference.
- **Avoid dynamic allocation on fatal error paths.** Use stack-allocated buffers. Use simple synchronization (Interlocked with spin-wait) instead of Monitor/lock.
- **Avoid thread-local objects with destructors in CoreCLR.** Destruction order is arbitrary. Tie lifetime to the CoreCLR Thread object. Prefer `PLATFORM_THREAD_LOCAL` from minipal over C++ `thread_local` in perf-critical paths.
- **Use `SET_UNALIGNED` macros for potentially unaligned writes.** In code generation stubs, use `SET_UNALIGNED_32/64` rather than direct pointer dereferencing.
- **Zero-initialize arrays and buffers that may be partially used.** Zero-init allocated arrays whose elements have destructors. Zero-init EH tables, C arrays, and similar structures.
- **Add static asserts for hardcoded structural offsets.** When using hardcoded offsets to access struct fields (especially in assembly), add static asserts to verify them.
- **Use minipal for new platform abstractions.** Use minipal (new) instead of PAL (legacy) for platform abstraction in new CoreCLR code. Use `ALTERNATE_ENTRY` (not `LOCAL_LABEL`) for assembly labels called from outside their function.
- **Use `JITDUMP` and `LOG` macros, not `printf`.** In JIT code use `JITDUMP`. In CoreCLR VM use `LOG()`/`LOGGING` defines. Do not use `printf` or `Console.WriteLine` in production native code.

### P/Invoke & Marshalling

- **Prefer 4-byte `BOOL` for native interop marshalling.** Use `UnmanagedType.Bool`. Verify P/Invoke return types match native signatures exactly—mismatches may work on 64-bit but fail on 32-bit/WASM.
