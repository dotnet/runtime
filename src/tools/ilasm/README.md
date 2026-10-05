# ILAssembler Build Workflow

This directory contains the ILAssembler tool and its build instructions.

## Pseudo Custom Attributes

The assembler lowers attributes such as `DllImport`, `MethodImpl`, `StructLayout`, and
`MarshalAs` into metadata flags and auxiliary tables. Decoded values follow C# compiler
conventions where applicable: unspecified method code types default to IL, invalid
P/Invoke calling conventions default to Winapi, and `StructLayout` maps `CharSet.None`
to Ansi. IL-specific directives and attribute targets remain supported.

`MethodImpl` options may use any bits that fit in the 16-bit metadata column except
the code-type bits, which must be set through `MethodCodeType`. This allows prototyping
new runtime flags without updating the assembler. The assembler does not apply C#
language restrictions such as limiting `MethodImplOptions.Async` to compiler-generated
methods.

Attributes with no metadata transform, including `Guid`, `InterfaceType`, `ClassInterface`,
`TypeLibVersion`, `ComCompatibleVersion`, and `AllowPartiallyTrustedCallers`, are emitted
unchanged without validating their arguments. Security attributes that affect metadata
are handled by the same lowering table as the other pseudo custom attributes.

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
