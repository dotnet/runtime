---
applyTo: "src/coreclr/jit/**"
---

# RyuJIT (JIT compiler) — Folder-Specific Guidance

## Code Review Guidelines

- **Do not request tests for JIT codebase improvements.** - JIT changes alter global codegen and are fully validated by existing end-to-end suites and differential testing. Individual PR regression tests are redundant and unnecessary unless it's a bug-fix PR that with a clear repro that is not covered by existing tests.
