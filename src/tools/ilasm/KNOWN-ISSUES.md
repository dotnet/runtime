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

## TLS RVA statics

Thread-local storage (TLS) RVA static fields (`.data tls`) are not
supported by the managed ilasm. The native ilasm emits a TLS directory
entry in the PE header for these, which the managed ilasm's PE builder
does not currently implement.

## Invalid metadata for complex generic type hierarchies

Some files with complex generic type hierarchies, covariant returns,
or interface method implementations produce metadata that causes ildasm
to hang or return errors. The managed ilasm compiles these files without
errors, but the output PE contains invalid metadata.

Known affected files:
- `src/tests/JIT/Regression/JitBlue/GitHub_17585/GitHub_17585.il`
- `src/tests/Loader/classloader/MethodImpl/CovariantReturns/UnitTest/OverrideMoreDerivedReturn.il`
- `src/tests/Loader/classloader/MethodImpl/CovariantReturns/UnitTest/UnitTestDelegates.il`
