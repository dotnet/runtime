# Managed IL Assembler - Known Issues

## Multi-file compilation with `#define` macros

When an ilproj has multiple `Compile` items where the first file defines
preprocessor macros (e.g., `#define ASSEMBLY_NAME "my_test"`) and the
second file uses those macros, the managed ilasm may fail to correctly
propagate the macro definitions across files.

**Workaround:** Combine the files into a single IL source file, or use
`#include` directives instead of separate `Compile` items.

**Affected patterns:** ~675 ilproj files in `src/tests/` that use a
two-file pattern with a small `_r.il` or `_d.il` file containing only
a `#define` and a main `.il` file using the defined macro.

## Preprocessor `#define` token gluing

When `#define` macros expand adjacent to other tokens, the preprocessor
may produce glued tokens that the ANTLR lexer treats as a single token
instead of separate tokens. Examples:

- `ldc.r4 float32(0xFF800000)` with `#define NEG_INF float32(0xFF800000)`
  produces `ldc.r4float32(...)` (opcode and type glued together)
- `#define FALSE "0"` causes `ldc.i4 FALSE` to produce `ldc.i4 "0"`
  (a QSTRING instead of INT32)

## TLS RVA statics

Thread-local storage (TLS) RVA static fields (`.data tls`) are not
supported by the managed ilasm. The native ilasm emits a TLS directory
entry in the PE header for these, which the managed ilasm's PE builder
does not currently implement.
