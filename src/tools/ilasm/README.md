# ILAssembler Build Workflow

This directory contains the ILAssembler tool and its build instructions.

## Diagnostics

Compiler diagnostics use `file(line,col) : severity ILAxxxx : message`, with
one-based source line and column numbers. Diagnostics do not include the source
file's contents. The `-ERR` option continues to suppress compiler diagnostics
while attempting to emit output despite errors.

Line starts are indexed lazily per source text and shared by its locations.
Each subsequent coordinate lookup uses binary search rather than rescanning the
file. Line breaks remain LF-based (including CRLF); columns count UTF-16 code
units, without expanding tabs.

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

For more details, see the main repository README or contact the maintainers.
