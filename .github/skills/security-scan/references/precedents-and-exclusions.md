# Precedents and Exclusions for dotnet/runtime

## Contents

- [Hard exclusions](#hard-exclusions--do-not-report)
- [Codebase-specific precedents](#precedents)

## Hard Exclusions — Do NOT Report

These categories produce excessive noise in dotnet/runtime and must be automatically excluded:

1. **~~Denial of Service (DOS)~~** — Now **in scope** for public API surface reachable by external callers. See [runtime-categories.md](runtime-categories.md#dos--resource-exhaustion). Still excluded for internal-only APIs and test code.
2. **Rate limiting** — Missing rate limits or throttling
3. **Resource leaks** — Unclosed files, connections, or memory
4. **Open redirects** — Low impact in this context
5. **Regex injection** — Unless compiled and processing attacker-controlled input
6. **Test-only code** — Files under `tests/`, `*Tests*` projects, test helpers, test data
7. **Documentation** — Findings in `.md`, `.txt`, or comment-only changes
8. **Log spoofing** — Writing unsanitized input to logs
9. **Missing audit logs**
10. **Environment variables and CLI flags** — Treated as trusted input
11. **Outdated dependencies** — Managed separately
12. **Client-side validation** — Server-side is responsible
13. **Insecure defaults in test/sample code** — Only flag in production code paths
14. **Memory safety in managed C# code** — Only flag in `unsafe` blocks, native interop, or C/C++ under `src/coreclr/` / `src/native/`

## Precedents

These judgment calls reduce false positives specific to this codebase:

1. **`Debug.Assert` is not a security boundary.** Asserts are stripped in release builds. Security checks must use exceptions or fail-fast.
2. **`internal` is not a security boundary.** Internal APIs can be accessed via reflection.
3. **`Span<T>` bounds checking is automatic.** The runtime checks bounds on span indexing. Missing manual bounds checks are not vulnerabilities.
4. **`ThrowHelper` methods are security-relevant.** When `ThrowHelper` is bypassed or conditions are wrong, the security check is ineffective.
5. **Native code under `src/coreclr/` and `src/native/` IS memory-unsafe.** C/C++ code requires manual bounds checking, null checks, and lifetime management.
6. **`System.Text.Json` source generators are trusted.** Generated serialization code doesn't need the same scrutiny as hand-written custom converters.
7. **Volatile/Interlocked correctness matters for security.** Race conditions in security checks (e.g., TOCTOU on permission flags) are real vulnerabilities.
