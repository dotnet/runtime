# API Design & Contracts

_Rules for public API surface, approval process, exception contracts, and obsoletion. Part of the [code-review skill](../SKILL.md)._

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
