---
applyTo: "src/**"
---

# Code Review -- General Guidance (all source areas)

Cross-cutting review criteria for any change under `src/`. Also apply the language file for the
code under review (`review-csharp`, `review-native`), `review-all-tests` for test changes, and
any matching area file (`jit`, `system-net-*`, `extensions-*`, `compression`, `cdac`). Where a
more specific file conflicts with a general one, the more specific file wins.

**Reviewer mindset:** Be polite but very skeptical. Your job is to help speed the review process for maintainers, which includes not only finding problems the PR author may have missed but also questioning the value of the PR in its entirety. Treat the PR description and linked issues as claims to verify, not facts to accept. Question the stated direction, probe edge cases, and don't hesitate to flag concerns even when unsure.

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
