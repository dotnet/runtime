---
applyTo: "src/coreclr/jit/**"
---

# RyuJIT (JIT compiler) — Folder-Specific Guidance

## Code Review Guidelines

- **Do not routinely request new targeted tests for pure refactors, mechanical cleanups, or other non-behavioral JIT codebase improvements.** JIT changes alter global codegen and are often validated by existing end-to-end suites and differential testing, so additional per-PR regression tests may not be needed for changes that do not affect observable behavior. However, targeted tests are appropriate and should be encouraged for bug fixes, changes with a clear repro, observable behavior changes, and previously untested edge cases that are not already covered by existing tests.

## Common false-positive review mistakes

- **`TYP_I_IMPL`/`TYP_U_IMPL` are target-dependent aliases, not distinct types.** They map to `TYP_LONG`/`TYP_ULONG` on 64-bit and `TYP_INT`/`TYP_UINT` on 32-bit. Do not claim `varTypeIsLong`/`varTypeIsInt` "miss" them; use `varTypeIsI`/`varTypeIsIntOrI` for native-int reasoning.
- **`varTypeIsLong` is width-based.** It is not "C# `long` only."
- **Read `varType*` helpers by contract, not name.** Check `vartype.h` and target guards first.
- **Do not mix width and signedness.** `TYP_BYTE`/`TYP_UBYTE` and `TYP_SHORT`/`TYP_USHORT` differ by signedness; width helpers include both.
- **Debug-only checks are often intentional.** Flag only if release logic is actually missing.
- **Conservative patterns are often deliberate.** Require concrete evidence (miscompile, ordering break, or CQ regression).
- **Importer late-expansion temps can be intentional.** Do not auto-flag as lost side effects or unnecessary temps.
- **Retyping to `TYP_VOID` can be correct after rewrites.** Check the full transform and side-effect shape before flagging.
- **`GTF_VAR_MOREUSES` is a conservative hint.** Over-marking (for example during address materialization) is not a default correctness bug.
- **Lowering helpers can rely on caller-proved preconditions.**
- **`GTF_SPILL` and `GTF_SPILLED` are different.** Review with LSRA def/use and `GT_RELOAD` context; transient set/clear can be intentional.
- **Raw-copy node bashing can be intentional.** Verify replacement invariants before filing generic `memcpy` issues.
- **JIT allocation is often arena-based.** Placement `new (compiler, CMK_*)` or `new (compiler->getAllocator(...))` without `delete` is usually expected.
- **Do not assume `|=` is always correct.** Some morph paths intentionally recompute flags with assignment to drop stale bits.
- **`GTF_DONT_CSE` is often intentional conservatism.** Do not remove/flag without proof.
- **`varDsc->lvNormalizeOnLoad() ? varDsc->TypeGet() : genActualType(varDsc)` is established.**
- **Liberal vs conservative VN differences are expected.**
- **VN aliasing rules are deliberate model choices.** Validate against the documented VN memory model before flagging.
- **`unreached()`/asserting defaults on internal enums can be intentional.** Do not request graceful handling without evidence of a reachable path.
- **Prefer a question over a claim when phase context is unclear.** Ask where the invariant is established instead of filing a bug by default.
- **If a concern depends on a phase/invariant, name both.** No phase/invariant => no actionable review comment.
- **Do not treat TODO/Cleanup comments as defects by default.** Flag only with correctness risk or measured CQ impact.
- **Do not re-raise a claim already resolved as by-design.** If prior discussion explains intent, either accept it or provide new evidence.
