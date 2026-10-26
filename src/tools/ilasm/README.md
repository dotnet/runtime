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

## Entry-point validation

Only one method may declare `.entrypoint`, and that method must be static.
A repeated directive (including a second directive in the same method) or an
instance entry point is an assembly error reported at the directive.
With `-ERROR`, the image is emitted: the first entry point is retained,
even if it is an invalid instance method. Compiler callers still receive the
diagnostics; the command line suppresses them in error-tolerant mode.

---

For more details, see the main repository README or contact the maintainers.
