# ILAssembler Build Workflow

This directory contains the ILAssembler tool and its build instructions.

## Build Instructions

### Regular Builds
For everyday development and regular builds, simply run:

```
./dotnet.sh build src/tools/ilasm/src/ILAssembler
```

### Updating Generated Files
If you modify any `.g4` grammar files (rare), you must regenerate the parser and related files:

```
./dotnet.sh build src/tools/ilasm/src/ILAssembler/gen
```

This will update the generated files before building the main project.

---

## Core Library Compatibility

As in native ilasm, `.mscorlib` or an assembly declaration named `mscorlib` identifies
a core library for the `System.Enum` sealing check. Its top-level `System.Enum`
definition remains unsealed despite extending `System.ValueType`. Other value types
retain their existing sealing diagnostics.

For more details, see the main repository README or contact the maintainers.
