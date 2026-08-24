---
applyTo: "src/coreclr/jit/**"
---

# RyuJIT (JIT compiler) — Folder-Specific Guidance

## Code Review Guidelines

- **Do not routinely request new targeted tests for pure refactors, mechanical cleanups, or other non-behavioral JIT codebase improvements.** JIT changes alter global codegen and are often validated by existing end-to-end suites and differential testing, so additional per-PR regression tests may not be needed for changes that do not affect observable behavior. However, targeted tests are appropriate and should be encouraged for bug fixes, changes with a clear repro, observable behavior changes, and previously untested edge cases that are not already covered by existing tests.
