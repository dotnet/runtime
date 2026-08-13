# ILAssembler

ILAssembler compiles declarations while ANTLR parses the input. The parser uses
`UnbufferedTokenStream` with parse-tree construction disabled. Namespace, type, top-level,
class-member and method-body structure is action-driven, while shared directives retain bounded
subtrees for their existing visitors. A complete document, declaration body or method body is
never retained. Rules such as `bytes` stream their content into an accumulator instead of building
a subtree at all.

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
| `GrammarActions.Declarations.cs` | Parser-driven top-level declaration visitor guards. |
| `GrammarActions.Declarations.Actions.cs` | Direct top-level declarations and shared-directive dispatch. |
| `GrammarActions.Instructions.cs` | Tree-free value instruction and method-item actions. |
| `GrammarActions.Instructions.References.cs` | Reference and signature instruction actions. |
| `GrammarActions.Literals.cs` | Literals, names and strings. |
| `GrammarActions.Manifest.cs` | Assembly, module, resource, vtable and typedef directives. |
| `GrammarActions.Marshalling.Actions.cs` | Synthesized native type and marshalling descriptor actions. |
| `GrammarActions.Marshalling.cs` | Marshalling visitor wrappers and P/Invoke conversion. |
| `GrammarActions.Members.cs` | Parser-driven class member visitor guards. |
| `GrammarActions.Members.Class.cs` | Class directives, generic parameter annotations and method overrides. |
| `GrammarActions.Members.Fields.cs` | Field declarations, attributes, layout, constants, marshalling and RVA data. |
| `GrammarActions.Members.PropertiesEvents.cs` | Property and event headers, bodies and accessors. |
| `GrammarActions.Members.Values.cs` | Internal synthesized member value model. |
| `GrammarActions.MethodHeaders.cs` | Method definition and signature materialization. |
| `GrammarActions.MethodHeaders.Actions.cs` | Method header, attribute, P/Invoke and generic parser actions. |
| `GrammarActions.MethodHeaders.Generics.cs` | Generic parameter and constraint synthesis and materialization. |
| `GrammarActions.MethodHeaders.Values.cs` | Internal synthesized method-header value model. |
| `GrammarActions.MethodBodies.cs` | Parser-driven method-body visitor guards. |
| `GrammarActions.MethodBodies.Directives.cs` | Direct method-body directives and parameter ownership. |
| `GrammarActions.MethodBodies.ExceptionHandling.cs` | Lexical scopes and synthesized exception regions. |
| `GrammarActions.MethodBodies.Values.cs` | Internal method-body directive and exception-region values. |
| `GrammarActions.Security.cs` | Declarative security conversion. |
| `GrammarActions.Signatures.cs` | Signature visitor compatibility wrappers. |
| `GrammarActions.Signatures.Actions.cs` | Signature grammar actions and repetition frames. |
| `GrammarActions.Signatures.References.cs` | Member-reference synthesis and materialization. |
| `GrammarActions.Signatures.Types.cs` | Type-signature materialization and encoding. |
| `GrammarActions.Signatures.Values.cs` | Internal synthesized signature value model. |
| `GrammarActions.Types.cs` | Namespace and type scope ownership and shared type conversion. |
| `GrammarActions.Types.Headers.cs` | Namespace and type-header materialization and visitor guards. |
| `GrammarActions.Types.Headers.Actions.cs` | Namespace, type attribute, base and interface parser actions. |
| `GrammarActions.Types.Headers.Values.cs` | Internal synthesized type-header value model. |
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

All namespace, type-header, top-level, type, signature, reference, marshalling, class-member,
method-header, method-body directive and exception-handling structure is action-driven.
`scopeBlock` records offsets under its context key without inspecting children. The remaining
`BeginSubtree` islands are the bounded shared roots `assemblyBlock`, `assemblyRefBlock`,
`exptypeBlock`, `manifestResBlock`, `dataDecl`, `fileDecl`, `vtableDecl`, `vtfixupDecl`, `secDecl`,
`extSourceSpec`, `languageDecl`, `typedefDecl`, `initOpt`, `customAttrDecl`, `customDescr`,
`customDescrWithOwner` and `customDescrInMethodBody`.

Semantic state that a rule pushes must be released from that rule's `finally` clause and be keyed on
the owning context, because ANTLR skips `@after` actions and the remainder of an alternative once a
rule reports a syntax error. Inline actions placed after a subrule reference are not a substitute:
they run only when the alternative completes.

Only the parser actions walk the structural rules. The recursive `ICILVisitor` entry points for
`decl`, `decls`, `nameSpaceHead`, `classHead`, their synthesized dependency rules, `classDecls`,
`methodDecls`, `scopeBlock` and the SEH rules throw `UnreachableException` so that there is exactly
one live traversal algorithm.

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
