# ILAssembler Build Workflow

This directory contains the ILAssembler tool and its build instructions.

## Build Instructions

The managed tool leaves custom attributes unchanged by default. Use `--pseudoattributes`
to lower recognized pseudo custom attributes into metadata flags and auxiliary tables.
This opt-in switch has no legacy `/` or single-dash spelling.

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
