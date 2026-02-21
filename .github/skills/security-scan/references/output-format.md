# Output Format

## Report Template

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

## Severity Guidelines

- **🔴 HIGH**: Directly exploitable — leads to RCE, data breach, auth bypass, or arbitrary code execution. Clear attack path exists.
- **🟡 MEDIUM**: Exploitable under specific conditions with significant impact. Requires particular configuration, timing, or access level.
- **Do not report LOW severity.** Better to miss theoretical issues than flood the report with noise.

## Confidence Scoring

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
