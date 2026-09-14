---
applyTo: "src/coreclr/**,src/native/corehost/**"
---

# Core runtime

Conventions for CoreCLR and native host changes. Also apply `conventions`, the language file
(`csharp` or `native`), `tests` for test changes, and `jit` for JIT changes.

## Correctness & Safety

- **Prefer correct-by-construction designs.** Prefer designs that are correct by construction (e.g., scanning IL) over manually maintained parallel data structures. A missed optimization is better than silent bad codegen.
- **Allocate on the correct loader allocator for collectibility.** When allocating runtime data structures for generic instantiations, use the correct loader allocator accounting for collectibility of type arguments.

## Performance & Allocations

- **Avoid LINQ and records in low-level compiler codebases.** In CG2/ILC and AOT tools, use direct loops instead of LINQ and readonly structs instead of records. Use concrete types over interfaces in private code.
