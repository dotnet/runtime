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

## Source line mapping

With debug information enabled, `.line` fixes the source span of subsequent instructions until another
source directive is encountered. `#line` also advances both line endpoints for each physical source
newline, including blank and comment lines, while keeping the columns unchanged. The hidden-sequence-point
marker remains hidden until another directive.

The line-only form with a double-quoted file name, such as `#line 42 "program.cs"`, assigns line 42 to
the following source line. Other forms, such as `#line 42 'program.cs'` or
`#line 42,44 : 1,8 'program.cs'`, anchor the specified range to the directive's own source line.
Instructions on the same source line share a sequence point. Includes maintain their own mappings;
returning from an include restores the including source's mapping. An effective span outside the
Portable PDB coordinate limits is diagnosed at the directive.

An omitted end column defaults to 65534 (`ushort.MaxValue - 1`) for compatibility with existing
metadata readers. Explicit columns are preserved, including the legal maximum 65535, which requires
a corrected reader. Hidden sequence points retain columns `0,0`.

For more details, see the main repository README or contact the maintainers.
