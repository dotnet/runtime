---
name: security-scan
description: Perform a security-focused review of code changes, thinking like a security researcher to find exploitable vulnerabilities. Use when asked to "security scan", "security review", "find vulnerabilities", "check for security issues", or "audit security". Also use when reviewing security-sensitive areas like cryptography, authentication, serialization, or input handling.
---

# Security Scan

Perform a deep, security-researcher-style audit of code changes in dotnet/runtime. This skill goes beyond pattern matching — it reasons about data flow, trust boundaries, and exploit paths the way an experienced security engineer would.

> 🚨 **Human-in-the-loop**: This skill identifies vulnerabilities and suggests fixes but NEVER auto-applies patches. All findings require human review and approval.

## When to Use This Skill

Use this skill when:
- Asked to perform a security review or audit of code changes
- Reviewing changes to security-sensitive areas (cryptography, auth, serialization, networking, input parsing)
- Checking a PR or branch for exploitable vulnerabilities before merge
- Investigating whether a code change introduces new attack surface
- Asked "is this change secure?", "find vulnerabilities", "security scan", or similar

## Gathering Context

### Step 1: Identify What Changed

Determine the scope of the review. Use one of these approaches depending on context:

**For a branch (most common — local review before commit/PR):**
```bash
git diff --name-only origin/HEAD...
git diff --merge-base origin/HEAD
```

**For a specific PR:**
Use the GitHub MCP tools to fetch the PR diff and file list:
- `pull_request_read` with method `get_diff` and `get_files`

**For a set of files the user specifies:**
Read those files directly.

Collect the full list of changed files and the complete diff content.

### Step 2: Repository Context Research

Before analyzing the changes, understand the security posture of the affected area. Use file exploration tools (`Glob`, `Grep`, `Read`) to:

1. **Identify security frameworks in use** — Look for existing validation helpers, sanitization patterns, `SecureString` usage, permission checks, `SafeHandle` usage, cryptographic API usage in the affected directories.
2. **Examine existing security patterns** — How does surrounding code handle untrusted input? What validation patterns are established? Are there `// SECURITY:` comments or `[SecurityCritical]` attributes nearby?
3. **Understand trust boundaries** — Where does user/external input enter the system? What crosses process, AppDomain, or serialization boundaries? What runs with elevated privileges?
4. **Check related security tests** — Look for existing security-focused tests (`*Security*`, `*Injection*`, `*Sanitiz*`, `*Untrusted*`) in the test projects for the affected libraries.

### Step 3: Analyze the Changes

Execute a three-phase analysis:

**Phase 1 — Comparative Analysis:**
- Compare new code against existing security patterns in the same area
- Identify deviations from established secure practices
- Look for inconsistent security implementations across similar code
- Flag code that introduces new attack surfaces

**Phase 2 — Vulnerability Assessment:**
- Examine each modified file for security implications
- Trace data flow from untrusted inputs to sensitive operations
- Look for privilege boundaries being crossed unsafely
- Identify injection points and unsafe deserialization
- Check for TOCTOU (time-of-check-time-of-use) issues in security-critical paths

**Phase 3 — Self-Critique and Verification:**
- For each potential finding, attempt to disprove it
- Check whether the issue is already mitigated by callers, callees, or framework guarantees
- Verify the finding against the full source file, not just the diff
- Assign a confidence score — only report findings with confidence ≥ 8/10

## Multi-Agent Verification

When the environment supports launching sub-agents (the `task` tool), use parallel verification to reduce false positives:

1. **Discovery agent**: Launch a `general-purpose` sub-agent that performs the full 3-phase analysis above. Provide it with the diff, changed file list, and the security categories to examine. Ask it to output structured findings.

2. **Verification agents**: For each finding from step 1, launch a parallel `explore` agent that independently assesses whether the finding is a true positive. Provide the finding details and instruct it to:
   - Read the full source file(s) involved
   - Check callers and callees for existing mitigations
   - Search for similar patterns elsewhere in the codebase
   - Assign a confidence score from 1-10 with justification

3. **Filter**: Only keep findings where the verification agent assigned confidence ≥ 8.

If the environment does not support sub-agents, perform the verification yourself sequentially.

## Security Categories to Examine

### dotnet/runtime-Specific Concerns

**Unsafe Code & Memory Safety:**
- `unsafe` blocks with pointer arithmetic, `Span<T>`/`Memory<T>` misuse
- Buffer overflows in native interop (`DllImport`, `LibraryImport`, `Marshal.*`)
- Use-after-free in `SafeHandle`/`CriticalHandle` implementations
- Stack buffer overflows via `stackalloc` without bounds checking
- `Unsafe.As<T>` type punning that violates type safety

**Serialization & Deserialization:**
- `BinaryFormatter`, `SoapFormatter`, or other insecure deserializers
- `TypeNameHandling` in JSON serialization allowing type injection
- Custom `TypeConverter` or `SerializationBinder` that don't restrict types
- XML deserialization without restricting allowed types (XXE, type injection)
- `System.Text.Json` custom converters that trust input type discriminators

**Cryptography:**
- Use of obsolete algorithms (MD5, SHA1 for security, DES, RC2, 3DES)
- Hardcoded keys, IVs, or salts
- Missing or incorrect padding modes
- Non-constant-time comparisons of secrets/MACs (use `CryptographicOperations.FixedTimeEquals`)
- Improper random number generation (using `Random` instead of `RandomNumberGenerator` for security)
- Certificate validation bypass (`ServerCertificateCustomValidationCallback` returning `true`)

**Input Validation & Injection:**
- Path traversal via unsanitized `Path.Combine` or `Path.GetFullPath`
- Command injection via `Process.Start` with unsanitized arguments
- LDAP injection, XPath injection in query construction
- Regex denial-of-service (ReDoS) with untrusted patterns — **only if the regex processes external input**
- Format string vulnerabilities in native code (`sprintf` without bounds)

**Authentication & Authorization:**
- Bypassing security checks via reflection or `BindingFlags.NonPublic`
- `AllowPartiallyTrustedCallers` misuse
- Elevation of privilege through assembly loading or code generation
- Missing permission demands on security-critical operations

**Networking & Web:**
- SSRF via user-controlled URLs (only if attacker can control host/protocol)
- TLS downgrade or missing certificate validation
- Cookie handling without `Secure`/`HttpOnly` flags
- HTTP header injection via unsanitized values

**Native Interop:**
- Buffer size mismatches between managed and native code
- Missing null checks on pointers returned from native calls
- Incorrect `MarshalAs` attributes leading to memory corruption
- Missing `SetLastError = true` on P/Invoke that checks `GetLastError`

### General Categories

**Injection Attacks:**
- SQL injection via string concatenation in queries
- Command injection in system calls or subprocesses
- XXE injection in XML parsing (missing `DtdProcessing.Prohibit`)
- Template injection in string formatting with untrusted input

**Data Exposure:**
- Sensitive data (keys, tokens, PII) in log output
- Debug information leaking in release builds
- Exception messages exposing internal paths or state
- Timing side channels leaking secret-dependent information

## Hard Exclusions — Do NOT Report

These categories produce excessive noise in dotnet/runtime and should be automatically excluded:

1. **Denial of Service (DOS)** — Resource exhaustion, algorithmic complexity attacks, memory/CPU exhaustion
2. **Rate limiting** — Missing rate limits or throttling
3. **Resource leaks** — Unclosed files, connections, or memory (not a security vulnerability)
4. **Open redirects** — Low impact in this context
5. **Regex injection** — Unless the regex is compiled and processes attacker-controlled input
6. **Test-only code** — Files under `tests/`, `*Tests*` projects, test helpers, and test data
7. **Documentation** — Findings in `.md`, `.txt`, or comment-only changes
8. **Log spoofing** — Writing unsanitized input to logs is not a vulnerability here
9. **Missing audit logs** — Not a vulnerability
10. **Environment variables and CLI flags** — Treated as trusted input; attacks requiring control of env vars are invalid
11. **Outdated dependencies** — Managed separately
12. **Client-side validation** — Client code is not trusted; server-side is responsible for validation
13. **Insecure defaults in test/sample code** — Only flag in production code paths
14. **Memory safety in managed C# code** — The CLR provides memory safety guarantees. Only flag memory issues in `unsafe` blocks, native interop, or C/C++ code under `src/coreclr/` or `src/native/`

## Precedents for dotnet/runtime

These judgment calls reduce false positives specific to this codebase:

1. **`Debug.Assert` is not a security boundary.** Asserts are stripped in release builds. Security checks must use exceptions or fail-fast.
2. **`internal` is not a security boundary.** Internal APIs can be accessed via reflection. Don't assume internal visibility prevents exploitation.
3. **`Span<T>` bounds checking is automatic.** The runtime checks bounds on span indexing. Manual bounds checks before span access are defense-in-depth, not vulnerabilities when missing.
4. **`ThrowHelper` methods are security-relevant.** When `ThrowHelper` is bypassed or conditions are wrong, the security check is ineffective.
5. **Native code under `src/coreclr/` and `src/native/` IS memory-unsafe.** C/C++ code requires manual bounds checking, null checks, and lifetime management. Flag memory safety issues here.
6. **`System.Text.Json` source generators are trusted.** Generated serialization code from source generators doesn't need the same scrutiny as hand-written custom converters.
7. **Volatile/Interlocked correctness matters for security.** Race conditions in security checks (e.g., TOCTOU on permission flags) are real vulnerabilities even though they're hard to exploit.

## Output Format

Present findings using this structure:

```
## 🔒 Security Scan — <scope description>

### Summary

**Findings**: <N high, N medium> | **Confidence threshold**: ≥ 8/10
**Verdict**: <✅ No issues found / ⚠️ Issues found — human review required>

---

### Findings

#### 🔴 HIGH: <Brief title> — `path/to/file.cs:42`

- **Category**: <e.g., command_injection, path_traversal, unsafe_deserialization>
- **Confidence**: <8-10>/10
- **Description**: <What the vulnerability is, in plain English>
- **Exploit scenario**: <Concrete attack path — how an attacker would exploit this>
- **Recommendation**: <Specific fix, with code suggestion if possible>

#### 🟡 MEDIUM: <Brief title> — `path/to/file.cs:87`

- **Category**: ...
- **Confidence**: ...
- **Description**: ...
- **Exploit scenario**: ...
- **Recommendation**: ...

---

### Files Reviewed

<List of files examined with brief notes on security-relevant observations>

### Methodology Notes

<Brief description of what was checked, tools/agents used, and any limitations>
```

### Severity Guidelines

- **🔴 HIGH**: Directly exploitable — leads to RCE, data breach, authentication bypass, or arbitrary code execution. Clear attack path exists.
- **🟡 MEDIUM**: Exploitable under specific conditions with significant impact. Requires particular configuration, timing, or access level.
- **Do not report LOW severity.** Focus on HIGH and MEDIUM only. Better to miss theoretical issues than flood the report with noise.

### Confidence Scoring

- **9-10**: Certain exploit path identified; verified against full source context
- **8-9**: Clear vulnerability pattern with known exploitation methods; verified no existing mitigation
- **Below 8**: Do not report. Too speculative.

## Final Checklist

Before presenting findings:

- [ ] Each finding verified against the **full source file**, not just the diff
- [ ] Each finding checked for **existing mitigations** in callers/callees
- [ ] Each finding has a **concrete exploit scenario**, not just a theoretical concern
- [ ] No findings in **excluded categories** (test code, docs, DOS, etc.)
- [ ] Confidence ≥ 8 for every reported finding
- [ ] All code suggestions are **syntactically correct** and would compile
