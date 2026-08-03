---
applyTo: "src/**"
---

# General Conventions (all source areas)

Code conventions for any change under `src/`, applied when authoring and when reviewing. Also
apply the language file for the code in question (`csharp`, `native`), `tests` for test changes,
and any matching area file (`core-runtime`, `jit`, `system-net-*`, `extensions-*`, `compression`,
`cdac`). Where a more specific file conflicts with a general one, the more specific file wins.

Pull-request process — scope, benchmark evidence, API approval, backport — is in
[`copilot-instructions.md`](/.github/copilot-instructions.md). Build and test workflow is in the
`build-and-test` skill. Review-only criteria are in `.github/skills/code-review/pr-assessment.md`.

## Change Scope & Justification

- **Prefer the simplest solution that works.** The burden of proof is on the more complex approach. Unnecessary abstraction, extra indirection, and elaborate solutions for marginal gains are a cost, not a feature.
- **Justify each addition.** New code, APIs, abstractions, and flags create a permanent maintenance obligation. If an addition can be avoided without sacrificing correctness or meaningful capability, avoid it.
- **Fix root cause, not symptoms or workarounds.** Investigate and fix the root cause rather than adding workarounds or suppressing warnings. Revert broken commits before layering fixes.
- **Don't bundle unrelated changes.** Keep each change to a single concern: no drive-by refactoring, no whitespace noise, no build artifacts. Large refactorings and mechanical renames belong in their own change, separate from logic changes.

## Consistency with Codebase Patterns

### Code Reuse & Deduplication

- **Extract duplicated logic into shared helper methods.** Fix improvements inside shared helpers so all callers benefit.
- **Move shared code to shared files, not duplicated across runtimes.** When identical code exists across CoreCLR and NativeAOT, move it to the shared partition (using `#if !MONO` if needed).
- **Use existing APIs instead of creating parallel ones.** Before introducing new types, enums, or helpers, check if existing ones serve the same purpose. Fix existing utilities rather than introducing duplicates.
- **Delete dead code and unused declarations aggressively.** Remove dead code, unnecessary wrappers, obsolete fields, and unused variables when encountered or when the only caller changes. Also remove helper methods, enum values, function declarations, and resx strings left unused by a removal.

### Established Conventions

- **Store error strings in `.resx`, not inline code.** Reference via the `SR` class. When removing code that uses a resx string, delete the unused string entry.
- **Preserve existing alphabetical ordering in modified lists.** When a PR adds or reorders entries in an alphabetized list—especially items within a `.csproj` item group, such as `Compile`, `ProjectReference`, and `PackageReference`—verify that the changed entries preserve the surrounding order. Flag only ordering regressions introduced by the PR; do not require unrelated cleanup of pre-existing unsorted entries. This also applies to lists of areas, configuration entries, resx entries, entrypoint/export lists, and ref source members.
- **Don't modify auto-generated files or `eng/common` manually.** Change the generator or source definition instead. Files in `eng/common` are synced from dotnet/arcade.
- **Use `DOTNET_` prefix for environment variables, not `COMPlus_`.** New runtime environment variables must use `DOTNET_` exclusively.
- **Match existing style in modified files.** The existing style in a file takes precedence over general guidelines. Do not change existing code for style alone.

### Runtime-Specific Patterns

- **Consider NativeAOT parity for runtime changes.** When changing CoreCLR behavior, verify whether the same change is needed for NativeAOT. Note: Mono and CoreCLR native code conventions differ significantly — do not assume they share the same rules.
- **Keep interpreter behavior consistent with the regular JIT.** Follow the same patterns, naming, error codes (`CORJIT_BADCODE`), and macros (`NO_WAY`). Use `FEATURE_INTERPRETER` guards.
- **Source generators: no file locks, diagnostics from analyzers only.** Generators should bypass invalid state gracefully. A separate analyzer should produce diagnostics.
- **Ref assembly conventions.** No `using` directives (fully qualify types), empty method bodies or `throw null`, genapi-style formatting, alphabetical member order. TFM-specific APIs go in separate files.

## Documentation & Comments

- **Comments should explain why, not restate code.** Delete comments like `// Get the types` that just duplicate the code in English. Don't include historical context about why code changed.
- **Delete or update obsolete comments when corresponding code changes.** Stale comments describing old behavior are worse than no comments. Update them when you touch the relevant code; leave unrelated stale comments to a dedicated cleanup pass.
- **Track deferred work with GitHub issues and searchable TODOs.** Reference a tracking issue in TODO comments with a consistent prefix (e.g., `TODO-Async:`). Remove ancient TODOs that will never be addressed.
- **Don't duplicate comments on interface implementations.** Documentation comments belong on the interface definition. Implementations should use `<inheritdoc/>` to avoid divergence.
- **Add XML doc comments on all new public APIs.** These seed the official API documentation on learn.microsoft.com. Properties should start with "Gets the ..." or "Gets or sets the ...". Do not add XML docs to test code.
- **Use SHA-specific or commit-based links in documentation.** Don't use branch-relative links that break when files move.
- **Reference specs and authoritative sources in implementation code.** When parsing signatures and metadata, cite the relevant spec section (e.g., ECMA-335). Link to relevant RFCs, papers, or repo-specific documentation (such as the ECMA-335 augments maintained in this repo). This applies broadly, not just to ECMA-335.
- **Use established terminology in user-facing text.** Do not expose internal type names, private field names, or codenames like "Roslyn" in public docs or error messages.
- **Retain copyright headers and license information.** All C# and C++ source files must include the standard license header, including test files. When porting from other projects, retain original copyright and update THIRD-PARTY-NOTICES.TXT.
