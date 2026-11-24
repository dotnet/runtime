# Repository Conventions and AI-Generated Code Rules

Always-loaded clarity, API-contract, and diff-hygiene checks for System.IO.Compression, System.Formats.Tar, and System.IO.Packaging changes.

---

## Diff Hygiene

| Check | What to look for |
|-------|-----------------|
| **Keep the diff on-topic** | Reordered members, moved files, rewritten unrelated tests, `CompatibilitySuppressions.xml` edits, visibility or return-type churn, and botched conflict resolution hide the real change and silently regress shipped behavior. Revert anything the fix does not need; if an existing test must change, explain why the old expectation was wrong. Leftover debug hacks, `?`-quality stubs, and half-finished TODO paths must not merge (`#115211`, `#120041`, `#122093`). |

---

## Naming and Comments

| Check | What to look for |
|-------|-----------------|
| **Name helpers for exactly what they touch** | A helper named `Write*` that only computes, a `*Unseekable*` helper that reads `Position`, or a `Prepare*`/`Compute*` header/metadata helper that secretly calls `Seek` misleads every caller about side effects and stream requirements. Name partial header helpers for the exact fields they write or validate, keep stream movement out of compute helpers, encode units (`Log2`, bytes vs. blocks, shifted mode bits), use PascalCase consts, and keep ctor parameter names equal to the property they set (`#72501`, `#84279`, `#121786`). |
| **Preserve governing why-comments** | Comments explaining a non-obvious seek, a sentinel default, a spec/tool-compat workaround, an ordering constraint, or a magic constant are the only record of why the code is shaped that way; deleting or not adding them invites a "cleanup" that reintroduces the bug. Keep or move the comment with the code it governs, add one for every new workaround or magic value, and link tracking issues with a full URL, not a bare number (`#103153`, `#123990`, `#77181`). |
| **Fix comments the change made stale** | Refactors leave comments describing the old branch, the opposite condition, a guarantee the code no longer has (e.g. "zlib-ng is always used"), or a format-specific claim on a shared test base. Wrong comments are worse than none. Update or delete every comment in touched code, and drop noise comments that restate the code or internal members' boilerplate triple-slash docs (`#129047`, `#127081`, `#74002`). |

---

## Code Shape

| Check | What to look for |
|-------|-----------------|
| **Delete dead and redundant code** | Unused fields, parameters, consts, default arguments, helpers, unreachable branches, and checks the BCL already performs (`ReadExactly` already validates, `Path.GetFullPath` already throws, buffers already zeroed, casts already implied) add maintenance cost and imply invariants that do not exist. Remove them in the same PR that made them dead (`#103153`, `#115211`, `#68106`). |
| **Share one implementation** | Copy-pasted header-writing blocks, duplicated ctors, `*WithPasswordsAsync` method clones, and private helpers that wrap a BCL call (`ReadBytesAsync` over `ReadAtLeastAsync`) drift apart over time. Delegate overloads to one core, reuse existing internal/common helpers, and do not add single-use types, wrappers, or new files where an existing one fits (`#84279`, `#118629`, `#75902`). |
| **Flatten control flow and derive state** | Nested `if` ladders without a final `else`/`default` silently accept new enum values; parallel flags (`_hasUnicodeEntryNameOrComment`, `IsZipCryptoEncrypted`) mirror state that can be derived and go stale. Prefer guard clauses, exhaustive `switch` with a throwing default, narrow variable scopes, and computed properties over cached booleans (`#122093`, `#59442`, `#117410`). |
| **Assert only true internal invariants** | `Debug.Assert` on caller-controlled input vanishes in Release and lets corrupt archives through; asserts that can never fire or are wrong (e.g. off-by-one length limits) mislead readers. Use `Debug.Assert` for implementation bugs only, throw for misuse or malformed data, and do not delete an existing assert without stating why the invariant no longer holds (`#76707`, `#75902`, `#67883`). |

---

## Public Contract

| Check | What to look for |
|-------|-----------------|
| **XML docs match actual behavior** | Every new public member needs triple-slash docs; `<exception>` tags must list exactly what is thrown and read as "thrown when..."; `<param>`/`<returns>` must match the signature; use `<see langword>`/`<see cref>` and `<inheritdoc>` for forwarding overloads. Remember docs for already-shipped members live in dotnet-api-docs and do not flow from source edits (`#108681`, `#105430`, `#103271`). |
| **Use SR resource strings correctly** | Hard-coded exception messages, near-duplicate resx entries, and messages that omit the offending value or name the wrong cause (e.g. CRC mismatch for a length mismatch) hurt diagnostics and localization. Add or reuse `Strings.resx` entries, include the relevant value/limit/format, and keep wording consistent with sibling messages (`#118720`, `#76707`, `#124766`). |
| **Throw the right exception in the right order** | `ArgumentException` for bad object state, `InvalidOperationException` for bad arguments, internal exception types deriving from `Exception`, or swallowed inner exceptions break callers' catch blocks. Validate arguments eagerly with `ArgumentNullException.ThrowIfNull`/`ArgumentOutOfRangeException.ThrowIf*` before any I/O, null before range, pass `paramName` and inner exceptions (`#122032`, `#114421`, `#123855`). |
| **Flag breaking behavior changes** | Changing which exception is thrown, entry order, metadata precedence (PAX attributes vs. properties), default fields written, or path normalization breaks existing callers even when tests pass. Call it out in the PR, prefer a compatible design, and route accepted breaks to the `breaking-change-doc` skill (`#115778`, `#60973`, `#123990`). |
| **Keep visibility and nullability honest** | `public` members on internal types, `private` to `internal` widening nobody uses, or nullable annotations that do not match runtime guarantees leak implementation details and mislead the compiler. Use the narrowest visibility, nested types for comparers, `[NotNullWhen]` on Try-patterns, and non-nullable parameters when null is never passed (`#122093`, `#70325`, `#72078`). |

---

## AI-Generated Code Pitfalls

| Check | What to look for |
|-------|-----------------|
| **Verify agent claims against Release behavior** | Generated fixes often add `Debug.Assert` guards, comments calling a test a "regression test", or explanations of prior behavior that were never checked. Ask what the Release build did before the change, delete self-congratulatory or speculative comments, and require evidence that the new test fails without the fix (`#123817`, `#122945`). |
| **Reject parallel files and placeholder stubs** | Agents tend to add catch-all "additional coverage" test files, duplicated `PlatformNotSupported` sources that are generated anyway, and stub branches left unimplemented. Fold tests into the existing file for that type, delete generated duplicates, and do not merge placeholder code (`#123989`, `#119575`, `#67883`). |
