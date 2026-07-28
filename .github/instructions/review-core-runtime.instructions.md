---
applyTo: "src/coreclr/**,src/native/corehost/**"
---

# Code Review -- Core runtime

Rules for reviewing CoreCLR and native host changes. Also apply `review-all-src`, the language
file (`review-csharp` or `review-native`), `review-all-tests` for test changes, and `jit` for
JIT changes.

These are review criteria. During code authoring or local experimentation, treat PR-level gates
such as motivation, benchmark evidence, and issue prerequisites as preparation guidance for a
ready-for-review PR, not as reasons to block exploratory work unless the user asks for review.

## Correctness & Safety

- **Prefer correct-by-construction designs.** Prefer designs that are correct by construction (e.g., scanning IL) over manually maintained parallel data structures. A missed optimization is better than silent bad codegen.
- **Allocate on the correct loader allocator for collectibility.** When allocating runtime data structures for generic instantiations, use the correct loader allocator accounting for collectibility of type arguments.

## Performance & Allocations

- **Avoid LINQ and records in low-level compiler codebases.** In CG2/ILC and AOT tools, use direct loops instead of LINQ and readonly structs instead of records. Use concrete types over interfaces in private code.

## PR Prerequisites

- **Start core component changes with an issue.** Changes to host, VM, or JIT should start with a GitHub issue describing the problem and motivation before submitting a PR.
