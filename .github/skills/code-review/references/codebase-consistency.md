# Consistency with Codebase Patterns

_Rules for PR hygiene, code reuse and deduplication, established conventions, and runtime-specific patterns. Part of the [code-review skill](../SKILL.md)._

## PR Hygiene

- **Keep PRs focused on their stated scope.** No accidental file modifications, no unrelated refactoring, no whitespace noise, no build artifacts. Each PR should serve a single purpose.
- **Do large refactorings and renames in separate PRs.** Separate no-diff refactors from functional changes. Mechanical renames should be separate from logic changes.
- **Merge to main first, then backport to release branches.** Use the `/backport` command. Backports to servicing are limited to security bugs, regressions, and reliability issues.

## Code Reuse & Deduplication

- **Extract duplicated logic into shared helper methods.** Fix improvements inside shared helpers so all callers benefit.
- **Move shared code to shared files, not duplicated across runtimes.** When identical code exists across CoreCLR and NativeAOT, move it to the shared partition (using `#if !MONO` if needed).
- **Use existing APIs instead of creating parallel ones.** Before introducing new types, enums, or helpers, check if existing ones serve the same purpose. Fix existing utilities rather than introducing duplicates.
- **Delete dead code and unused declarations aggressively.** When removing code, also remove helper methods, enum values, function declarations, and resx strings that are no longer used.

## Established Conventions

- **Store error strings in `.resx`, not inline code.** Reference via the `SR` class. When removing code that uses a resx string, delete the unused string entry.
- **Sort lists and entries alphabetically.** Lists of areas, configuration entries, resx entries, entrypoint/export lists, and ref source members should be maintained in alphabetical order.
- **Don't modify auto-generated files or `eng/common` manually.** Change the generator or source definition instead. Files in `eng/common` are synced from dotnet/arcade.
- **Use `DOTNET_` prefix for environment variables, not `COMPlus_`.** New runtime environment variables must use `DOTNET_` exclusively.
- **Match existing style in modified files.** The existing style in a file takes precedence over general guidelines. Do not change existing code for style alone.
- **Prefer `sizeof` over `Unsafe.SizeOf` consistently.** A pass was done to replace all `Unsafe.SizeOf` uses. Do not reintroduce them.

## Runtime-Specific Patterns

- **Consider NativeAOT parity for runtime changes.** When changing CoreCLR behavior, verify whether the same change is needed for NativeAOT.
- **Keep interpreter behavior consistent with the regular JIT.** Follow the same patterns, naming, error codes (`CORJIT_BADCODE`), and macros (`NO_WAY`). Use `FEATURE_INTERPRETER` guards.
- **Source generators: no file locks, diagnostics from analyzers only.** Generators should bypass invalid state gracefully. A separate analyzer should produce diagnostics.
- **Ref assembly conventions.** No `using` directives (fully qualify types), empty method bodies or `throw null`, genapi-style formatting, alphabetical member order. TFM-specific APIs go in separate files.
