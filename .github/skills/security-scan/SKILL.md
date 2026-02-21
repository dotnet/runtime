---
name: security-scan
description: Perform a security-focused review of code in dotnet/runtime, thinking like a security researcher to find exploitable vulnerabilities. Use when asked to "security scan", "security review", "find vulnerabilities", "check for security issues", or "audit security". Supports reviewing diffs, specific files, or entire directories. Analyzes API contracts, serializer safety, and DOS surface. Anti-pattern catalog is reusable for downstream repos like ASP.NET Core.
---

# Security Scan

Security-researcher-style audit for dotnet/runtime. Reasons about data flow, trust boundaries, exploit paths, and API contract abuse.

> 🚨 **Human-in-the-loop**: Identifies vulnerabilities and suggests fixes but NEVER auto-applies patches. All findings require human review.

## Step 1: Determine Scope and Triage

Infer the review scope from the user's request:

| User intent | Scope | How to gather files |
|---|---|---|
| Review my changes / PR review (default) | **diff** | `git diff --merge-base origin/HEAD` or PR diff via MCP |
| Scan these files | **files** | Read the specified files directly |
| Audit this directory / library | **directory** | Run triage script, then analyze prioritized files |

**For directory scope**, run the triage script first to focus on security-relevant files:

```bash
python .github/skills/security-scan/scripts/scan_security_surface.py <path> --json
```

This outputs a prioritized file list with signal annotations (unsafe code, serialization, DOS surface, public API entry points, etc.). Focus deep analysis on `high` and `medium` priority files only.

For **diff** scope, also collect the full list of changed files with `git diff --name-only origin/HEAD...`.

For large-scale scans (multiple libraries, 50+ files), see [references/coordination.md](references/coordination.md) for partitioning, SQL tracking, and subagent delegation.

## Step 2: Research Context

Before analyzing code, understand the security posture of the affected area using `Glob`, `Grep`, and `Read`:

1. **Security patterns in use** — validation helpers, `SafeHandle` usage, `[SecurityCritical]` attributes, `// SECURITY:` comments
2. **Trust boundaries** — where external input enters; what crosses process, AppDomain, or serialization boundaries
3. **Related security tests** — search for `*Security*`, `*Injection*`, `*Sanitiz*`, `*Untrusted*` in nearby test projects

## Step 3: Analyze

Execute a multi-phase analysis:

**Phase 1 — Vulnerability assessment:** Trace data flow from untrusted inputs to sensitive operations. Check for injection, unsafe deserialization, privilege boundary crossings, TOCTOU issues, and DOS vectors.

**Phase 2 — Contract analysis:** For public APIs in scope, check for implicit contracts, missing validation, inconsistent overloads, and cross-component contract mismatches. See [references/contract-analysis.md](references/contract-analysis.md).

**Phase 3 — Serializer audit:** For any serializer usage flagged by triage, apply the per-serializer checklist. See [references/serializer-audit.md](references/serializer-audit.md).

**Phase 4 — Self-critique:** For each potential finding, attempt to disprove it. Verify against the full source file, check for mitigations in callers/callees. Only keep findings with confidence ≥ 8/10.

**Reference materials** (read as needed):
- **Vulnerability categories**: [references/runtime-categories.md](references/runtime-categories.md)
- **Exclusions and precedents**: [references/precedents-and-exclusions.md](references/precedents-and-exclusions.md)
- **Known anti-patterns**: [references/anti-patterns/README.md](references/anti-patterns/README.md)

## Step 4: Multi-Agent Verification

When the `task` tool is available, use parallel verification:

1. **Discovery agent**: Launch a `general-purpose` sub-agent with the diff/files, changed file list, and the categories from [references/runtime-categories.md](references/runtime-categories.md). Ask it to output structured findings.

2. **Verification agents**: For each finding, launch a parallel `explore` agent to independently verify it — read full source files, check callers/callees for mitigations, search for similar patterns. Each assigns a confidence score 1-10.

3. **Filter**: Only keep findings with verification confidence ≥ 8.

Without sub-agents, perform verification sequentially.

For large-scale scans (multiple libraries, 5+ subagents), see [references/coordination.md](references/coordination.md) for delegation templates, SQL-based progress tracking, finding aggregation, and caching.

## Step 5: Report

Format findings per the template in [references/output-format.md](references/output-format.md).

Record any new anti-patterns discovered during the scan in [references/anti-patterns/README.md](references/anti-patterns/README.md) for reuse in downstream repo scans.
