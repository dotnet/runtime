# AI and Repository Conventions

Always-loaded rules for diff hygiene, readability, public API, exceptions, and diagnostics in networking code, plus the pitfalls most common in AI-generated changes.

---

## Scope & Diff Hygiene

| Check | What to look for |
|-------|-----------------|
| **Keep the diff on-task** | Edits to unrelated files, CI/Helix YAML defaults, `.globalconfig`, or existing tests; leftover `Console.WriteLine`, `Thread.Sleep`, personal fork URLs, or half-finished stubs; code a rebase silently reverted. Each widens review and regresses shared infra. Revert anything the change does not need, ship separate concerns in separate PRs, and re-diff after every rebase. (`#81841`, `#106599`, `#123234`) |

---

## Naming & Comments

| Check | What to look for |
|-------|-----------------|
| **Names carry units, intent, and .NET casing** | `timeout`, `size`, `data`, `temp` without units (ms/s/bytes/chars) or meaning; fields not `_camelCase`/`s_`/`t_`; async methods missing `Async`, `*Core`/`Try*` misused; names left stale after a rename; bare magic numbers instead of named `const`s or BCL constants. Misread units cause real timeout and buffer bugs. Rename to `timeoutMilliseconds`, `bufferLength`, etc. and name every protocol constant. (`#103769`, `#86983`, `#74579`, `#64733`) |
| **Comments state the truth and the why** | Comments describing pre-PR behavior, the opposite branch, or the wrong platform/RFC rule actively mislead. Conversely, handle ownership, memory-ordering/race invariants, RFC or OS-version workarounds, and `#pragma`/`TODO`/suppressions need a reason and a tracking-issue link. Update or delete stale comments in the same change; add a short why where the code is non-obvious. (`#109690`, `#100506`, `#129475`, `#73238`, `#74953`) |

---

## Simplicity & Dead Code

| Check | What to look for |
|-------|-----------------|
| **Delete dead code** | Uncalled private/internal helpers, unreachable branches, dead `#if` blocks, unused fields/parameters/locals, oversized `stackalloc`, or helpers added "for later". Dead code hides real behavior and rots. Remove it; add members only when a caller lands in the same PR. (`#68288`, `#126669`, `#106964`) |
| **Drop redundant checks and casts** | `== true`, casts to the same type, default field initializers, null/bounds checks the caller already guarantees, pre-checks before a BCL call that validates itself (`DangerousAddRef` already throws `ObjectDisposedException`), nested duplicate `#if`. They obscure which checks matter. Delete them, or turn real preconditions into `Debug.Assert`. (`#88550`, `#65249`, `#97537`, `#74669`) |
| **Idiomatic flow, scope, and members** | Deep `if`/`else` nesting, duplicated `catch`/cleanup branches, expensive checks before cheap ones, `if` chains that should be an exhaustive `switch` expression, locals declared far from use, `as`+null instead of patterns or `??=`, members that should be `static`/`readonly`, or duplicate state fields that can desynchronize. Use guard clauses, patterns, and one source of truth for state. (`#64369`, `#71432`, `#85167`, `#101250`, `#113229`) |

---

## API Surface, Docs, Exceptions & Breaking Changes

| Check | What to look for |
|-------|-----------------|
| **XML docs state the real contract** | New public members without `///` docs; `<param>` names that do not match; missing `<exception>` tags; summaries that omit disposal, thread-safety, or platform behavior; plain `true`/`null` instead of `<see langword="..."/>`; non-standard wording ("Initializes a new instance of..."). Shipped docs are generated from these. Document the observable contract with correct `cref`/`langword` markup. (`#74945`, `#130366`, `#118095`) |
| **Throw the documented exception, never swallow** | `Exception`, raw interop errors, or a lost `paramName`/`InnerException`/`SocketErrorCode`; wrong precedence of `ArgumentNullException` vs `ObjectDisposedException` vs `PlatformNotSupportedException`; manual `if (x is null) throw` instead of `ThrowIfNull`/`ObjectDisposedException.ThrowIf`; bare `catch` for control flow; one exception instance thrown twice; English literals instead of `SR.*` resources. (`#107096`, `#71757`, `#80066`, `#102416`, `#123934`) |
| **Behavior changes are breaking changes** | Changed defaults, disposal ownership, callback counts, stream position, enum ordinals, error mappings, or a new exception on input that used to work. Callers break silently. Prefer keeping behavior; if the change is intended, call it out in the PR, add tests pinning old and new behavior, and follow up with the `breaking-change-doc` skill after merge. (`#63851`, `#125220`, `#86229`) |
| **Minimal, consistent public surface** | New `public`/`protected` members without an approved API proposal, `internal` members wider than needed, `LibraryImport` exposed instead of a typed wrapper, or option enums and parameter orders that differ from sibling BCL APIs. Public API needs an approved `api-proposal` before merge; until then keep it `internal`. Match existing naming and parameter order. (`#88970`, `#65591`, `#84383`) |

---

## Diagnostics, Nullability & Invariants

| Check | What to look for |
|-------|-----------------|
| **Telemetry follows its schema** | `[Event]` IDs/opcodes that do not match the method, `Console.WriteLine` used as logging, `Activity` parented to the wrong span, metrics with non-standard units (`s` vs `ms`) or tag values, or tags read from mutable request state. Dashboards and OpenTelemetry consumers depend on these. Follow the documented semantic conventions and keep event IDs stable. (`#84036`, `#85447`) |
| **Nullability annotations are truthful** | `?` missing on members that can be null, `!` hiding a real null path, missing `[MemberNotNull]`/`[NotNullWhen]`, or unguarded `null` reaching a non-nullable API. Annotations are public contract and the compiler trusts them. Annotate what actually happens; replace `!` with a check or a `Debug.Assert`. (`#82779`, `#74863`) |
| **Assert invariants, throw on reachable errors** | Missing `Debug.Assert` for internal preconditions; asserts that are inverted or fire on valid zero/disposed states; release `throw` for impossible states, or `Debug.Assert`/silent `false` for errors user input can trigger. Use `Debug.Assert` with a message for invariants and real exceptions for anything reachable. (`#65249`, `#90253`, `#125220`) |

---

## AI-Generated Code Pitfalls

| Check | What to look for |
|-------|-----------------|
| **No unrequested refactors** | Signature, return-type, visibility, or file-layout changes that the fix does not need, and reformatted or "modernized" existing tests. They bloat the diff and can break callers and test intent. Keep only the lines required for the fix; propose cleanups separately. (`#102144`, `#128355`) |
| **No invented or reimplemented APIs** | Calls to members that do not exist, or hand-rolled copies of helpers that already live in `Common` (`ReadAtLeast`, pinning helpers, buffer types) or in a sibling PAL/protocol version. Duplicates drift and miss fixes. Verify every API exists for the target TFM and reuse the shared helper. (`#87514`, `#120612`, `#68834`) |
| **No speculative abstraction** | Single-implementation interfaces, one-caller pass-through helpers, wrapper class hierarchies, or unused generic parameters. They add indirection without behavior. Inline until a second real consumer exists. (`#124374`, `#82779`) |
| **No restating comments or placeholders** | Comments that repeat the code (`// increment counter`), commented-out code, unfilled template text, or `TODO` stubs left in place of an implementation. They add noise and hide unfinished work. Delete them or finish the code. (`#128325`, `#128856`) |
| **Never silently weaken assertions** | Tests whose expected value was loosened, an inequality relaxed, a platform exemption added, or a `Debug.Assert` condition changed just to make CI pass. That hides the bug the test exists to catch. Fix the product, or justify the new expectation explicitly in the PR. (`#111710`, `#73283`) |
