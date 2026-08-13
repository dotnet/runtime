# ILAssembler

ILAssembler compiles declarations while ANTLR parses the input. The parser uses
`UnbufferedTokenStream` with parse-tree construction disabled. Grammar actions retain bounded
subtrees only while `GrammarActions` processes a declaration or method-body item, so a complete
document or method body is never retained. Rules such as `bytes` stream their content into an
accumulator instead of building a subtree at all.

`GrammarActions` is a single `internal sealed partial class` split across
`src/ILAssembler/Actions/GrammarActions.*.cs`:

| File | Contents |
| ---- | -------- |
| `GrammarActions.cs` | Per-document lifecycle. |
| `GrammarActions.BuildImage.cs` | PE and portable PDB construction. |
| `GrammarActions.Bytes.cs` | `bytearray` accumulation. |
| `GrammarActions.Conversions.cs` | `GrammarResult`, shared state and core visitor plumbing. |
| `GrammarActions.CustomAttributes.cs` | Custom attribute values and serialization. |
| `GrammarActions.Data.cs` | Data and blob declarations. |
| `GrammarActions.Debug.cs` | Source and debug directives. |
| `GrammarActions.Declarations.cs` | Top-level `decl` dispatch and conversion. |
| `GrammarActions.Instructions.cs` | Tree-free value instruction and method-item actions. |
| `GrammarActions.Instructions.References.cs` | Reference and signature instruction actions. |
| `GrammarActions.Literals.cs` | Literals, names and strings. |
| `GrammarActions.Manifest.cs` | Assembly, module, resource, vtable and typedef directives. |
| `GrammarActions.Marshalling.cs` | Native type and marshalling conversion. |
| `GrammarActions.Members.cs` | Class member dispatch and conversion. |
| `GrammarActions.MethodBodies.cs` | Method, lexical scope and exception-handling state and conversion. |
| `GrammarActions.Security.cs` | Declarative security conversion. |
| `GrammarActions.Signatures.cs` | Signature visitor compatibility wrappers. |
| `GrammarActions.Signatures.Actions.cs` | Signature grammar actions and repetition frames. |
| `GrammarActions.Signatures.References.cs` | Member-reference synthesis and materialization. |
| `GrammarActions.Signatures.Types.cs` | Type-signature materialization and encoding. |
| `GrammarActions.Signatures.Values.cs` | Internal synthesized signature value model. |
| `GrammarActions.Types.cs` | Namespace and type scopes and declaration conversion. |
| `GrammarActions.Types.References.cs` | Type-name synthesis and resolution. |

`CILParser.Actions.cs` holds the parse-tree mode helpers the grammar calls.

## Rules for grammar actions

Parser actions in `src/ILAssembler/gen/CIL.g4` must remain thin; they call a single `Actions` method
and nothing else. Compilation orchestration belongs in the `GrammarActions` partial-class files.

Instruction dispatch and every operand root run with parse-tree construction disabled.
Type, signature and reference rules synthesize compact semantic values that their existing
`VisitX(context).Value` wrappers materialize. The generated ANTLR context classes are public, while
ILAssembler entities and signature implementation values remain internal, so generated return
slots use `object` where an internal value or array crosses that boundary and `GrammarActions`
provides the strongly typed accessors.

`marshalClause` is the remaining signature-layer parse-tree island. It retains only the bounded
`marshalBlob`/`nativeType` subtree until its containing signature is materialized.

Semantic state that a rule pushes must be released from that rule's `finally` clause and be keyed on
the owning context, because ANTLR skips `@after` actions and the remainder of an alternative once a
rule reports a syntax error. Inline actions placed after a subrule reference are not a substitute:
they run only when the alternative completes.

Only the parser actions walk the structural rules. The recursive `ICILVisitor` entry points for
`decls`, `classDecls`, `methodDecls`, `scopeBlock` and the SEH rules throw `UnreachableException` so
that there is exactly one live traversal algorithm.

## Build

```
./dotnet.sh build src/tools/ilasm/src/ILAssembler
```

On Windows, use `.\dotnet.cmd` instead of `./dotnet.sh`.

## Updating generated parser files

After modifying `CIL.g4`, regenerate the checked-in ANTLR output before building ILAssembler:

```
./dotnet.sh build src/tools/ilasm/src/ILAssembler/gen
./dotnet.sh build src/tools/ilasm/src/ILAssembler
```

Do not edit generated `CIL*.cs` or `.interp` files manually.
