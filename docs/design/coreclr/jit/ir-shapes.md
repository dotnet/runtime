# RyuJIT IR shapes

Most "what shape is legal here?" questions reduce to two questions: **which IR form**, and
**which phase**? The long-form answer is [Overview of the IR](ryujit-overview.md#overview-of-the-ir).

|                | HIR (front end)                                   | LIR (back end) |
|----------------|---------------------------------------------------|---|
| Block contents | linked `Statement`s, each with an expression tree | flat doubly-linked `GenTree` list |
| Order          | tree order (`GTF_REVERSE_OPS` flips operands)     | `gtNext`/`gtPrev` order |
| Tree temps     | SDSU: single def, single use; no cross-block uses | same, plus defs must precede uses in the block |
| Span           | import → morph → optimizations                    | after [rationalization](ryujit-overview.md#rationalization), refined by [lowering](ryujit-overview.md#lowering) |
| Allowed nodes  | node lacks `DBK_NOTLIR`                           | node lacks `DBK_NOTHIR` |

`gtNext`/`gtPrev` are only authoritative in LIR; see
[phase-dependent invariants](ryujit-overview.md#phase-dependent-properties-and-invariants-of-the-ir).

## The exhaustive list

Don't duplicate it — [`gtlist.h`](../../../../src/coreclr/jit/gtlist.h) is the table:
`GTNODE(name, struct, commutative, illegal-as-VN-func, kind)`.

- `GTK_LEAF` (0 operands), `GTK_UNOP` (1), `GTK_BINOP` (2), `GTK_SPECIAL` (calls, phis, etc.).
- `GTK_EXOP` = subclass with extra fields; `GTK_NOVALUE` / `GTK_STORE` = side-effecting node.
- Debug flags in [`gentree.h`](../../../../src/coreclr/jit/gentree.h): `DBK_NOTHIR` (LIR-only,
  e.g. `GT_LEA`, `GT_CMP`, `GT_PUTARG_REG`), `DBK_NOTLIR` (HIR-only, e.g. `GT_COMMA`, `GT_QMARK`,
  `GT_COLON`), `DBK_NOCONTAIN`.

The opcode enum and per-node structs are generated from that table
([`gentreeopsdef.h`](../../../../src/coreclr/jit/gentreeopsdef.h),
[`gtstructs.h`](../../../../src/coreclr/jit/gtstructs.h)).
`GenTree::OperIsLIR()` and `LIR::Range::CheckLIR()`
([`lir.cpp`](../../../../src/coreclr/jit/lir.cpp)) enforce the shape rules in Debug builds.

## Common shapes

| Shape                               | Examples                                                                                                                   | More detail |
|-------------------------------------|----------------------------------------------------------------------------------------------------------------------------|---|
| 0 operands                          | `GT_CNS_*`, `GT_LCL_VAR`, `GT_LCL_FLD`, `GT_LCL_ADDR`, `GT_PHI_ARG`, `GT_NOP`                                              | [locals](ryujit-overview.md#local-var-descriptors) |
| 1 operand                           | `GT_NEG`, `GT_CAST`, `GT_BITCAST`, `GT_IND`, `GT_BLK`, `GT_STORE_LCL_VAR`, `GT_RETURN`, `GT_SWITCH`                        | [`gtlist.h`](../../../../src/coreclr/jit/gtlist.h) |
| 2 operands                          | arithmetic, compares, `GT_STOREIND`, `GT_STORE_BLK`, `GT_INDEX_ADDR`; `GT_COMMA` (HIR only), `GT_LEA` (LIR only)           | [rationalization](ryujit-overview.md#rationalization), [lowering](ryujit-overview.md#lowering) |
| condition + `GT_COLON` (true/false) | `GT_QMARK` (HIR only), `GT_SELECT`                                                                                         | [qmark elimination](ryujit-overview.md#eliminate-qmarks) |
| variable / special                  | `GT_CALL` + argument list, `GT_PHI` + `GT_PHI_ARG`, `GT_HWINTRINSIC`, `GT_FIELD_LIST`                                      | [call morphing](jit-call-morphing.md), [multi-register returns](multi-reg-call-nodes.md), [SSA/VN](ryujit-overview.md#ssa-vn) |
| backend-only                        | `GT_CMP` / `GT_JCC` / `GT_SETCC`, `GT_PUTARG_REG` / `GT_PUTARG_STK`, `GT_COPY` / `GT_RELOAD`, `GT_PHYSREG`, `GT_IL_OFFSET` | [lowering](ryujit-overview.md#lowering), [codegen](ryujit-overview.md#code-generation) |

Struct and field node shapes have changed over time; [First-class structs](first-class-structs.md)
is a design document and may lag the code. Trust [`gtlist.h`](../../../../src/coreclr/jit/gtlist.h).

## Adding a node

Add a `GTNODE` row with its struct and flags, decide whether it is HIR-only, LIR-only, or both,
and keep LIR defs before uses with a single use per tree temp. Add prose here only for rules the
table cannot express.
