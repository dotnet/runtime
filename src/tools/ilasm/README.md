# ILAssembler

ILAssembler compiles declarations while ANTLR parses the input. The parser uses
`UnbufferedTokenStream` with parse-tree construction disabled. Namespace, type, top-level,
class-member, method-body and shared-directive structure is action-driven. A complete document,
declaration body or method body is never retained. Rules such as `bytes` stream their content into
an accumulator instead of building a subtree at all. The generator emits neither listeners nor
visitors; parser actions own traversal.

## Public contract

`src/ILAssembler/ref/ILAssembler.csproj` defines the supported compiler API as a custom reference
assembly, following the same pattern as Mono.Linker. Project references compile against this
contract by default, while the implementation assembly remains the runtime asset.

ANTLR-generated parser types and the preprocessing/string helpers used by implementation tests are
intentionally absent from the contract. Tests opt into the implementation assembly with
`SkipUseReferenceAssembly`.

`GrammarActions` is a single `internal sealed partial class` split across
`src/ILAssembler/Actions/GrammarActions.*.cs`:

| File | Contents |
| ---- | -------- |
| `GrammarActions.cs` | Per-document lifecycle. |
| `GrammarActions.BuildImage.cs` | PE and portable PDB construction. |
| `GrammarActions.Bytes.cs` | `bytearray` accumulation. |
| `GrammarActions.Conversions.cs` | Diagnostics and shared state. |
| `GrammarActions.CustomAttributes.Actions.cs` | Custom attribute descriptors, declarations and blob lists. |
| `GrammarActions.CustomAttributes.Sequences.cs` | Custom attribute scalar-array sequence synthesis. |
| `GrammarActions.CustomAttributes.Serialization.cs` | Serialized attribute values and field/parameter initializers. |
| `GrammarActions.Data.cs` | Streaming mapped-data declarations, labels and reference fixups. |
| `GrammarActions.Debug.cs` | Direct source-location, document and language directives. |
| `GrammarActions.Declarations.Actions.cs` | Direct top-level declarations and shared-directive dispatch. |
| `GrammarActions.Instructions.cs` | Tree-free value instruction and method-item actions. |
| `GrammarActions.Instructions.References.cs` | Reference and signature instruction actions. |
| `GrammarActions.Literals.cs` | Literals, names and strings. |
| `GrammarActions.Manifest.Assembly.cs` | Assembly definitions, identity, keys, security and attributes. |
| `GrammarActions.Manifest.ExportedTypes.cs` | Exported-type headers, implementations and attributes. |
| `GrammarActions.Manifest.Files.cs` | Assembly file declarations and entry points. |
| `GrammarActions.Manifest.References.cs` | Assembly references, identities, keys and hashes. |
| `GrammarActions.Manifest.Resources.cs` | Embedded and external manifest resources. |
| `GrammarActions.Manifest.Typedefs.cs` | Type, member and custom-attribute aliases. |
| `GrammarActions.Manifest.VTable.cs` | Vtable fixup declarations and flags. |
| `GrammarActions.Marshalling.Actions.cs` | Synthesized native type and marshalling descriptor actions. |
| `GrammarActions.Members.Class.cs` | Class directives, generic parameter annotations and method overrides. |
| `GrammarActions.Members.Fields.cs` | Field declarations, attributes, layout, constants, marshalling and RVA data. |
| `GrammarActions.Members.PropertiesEvents.cs` | Property and event headers, bodies and accessors. |
| `GrammarActions.MethodHeaders.cs` | Method definition and signature materialization. |
| `GrammarActions.MethodHeaders.Actions.cs` | Method header, attribute, P/Invoke and generic parser actions. |
| `GrammarActions.MethodHeaders.Generics.cs` | Generic parameter and constraint synthesis and materialization. |
| `GrammarActions.MethodBodies.cs` | Label validation and method-name parsing. |
| `GrammarActions.MethodBodies.Directives.cs` | Direct method-body directives and parameter ownership. |
| `GrammarActions.MethodBodies.ExceptionHandling.cs` | Lexical scopes and synthesized exception regions. |
| `GrammarActions.Security.cs` | Synthesized declarative-security values and permission sets. |
| `GrammarActions.Signatures.cs` | Member and type signature materialization helpers. |
| `GrammarActions.Signatures.Actions.cs` | Signature grammar actions and typed aggregation helpers. |
| `GrammarActions.Signatures.References.cs` | Member-reference synthesis and materialization. |
| `GrammarActions.Signatures.Types.cs` | Type-signature materialization and encoding. |
| `GrammarActions.Types.cs` | Namespace and type scope ownership and shared type conversion. |
| `GrammarActions.Types.Headers.cs` | Namespace and type-header materialization. |
| `GrammarActions.Types.Headers.Actions.cs` | Namespace, type attribute, base and interface parser actions. |
| `GrammarActions.Types.References.cs` | Type-name synthesis and resolution. |

The hand-written `public partial CILParser` semantic model is split by feature:

| File | Contents |
| ---- | -------- |
| `CILParser.SemanticValues.CustomAttributes.cs` | Custom attribute, serialization and initializer values. |
| `CILParser.SemanticValues.Declarations.cs` | Type/member headers and their context-owned builders. |
| `CILParser.SemanticValues.Manifest.cs` | Assembly, file, exported-type, resource and typedef values. |
| `CILParser.SemanticValues.Marshalling.cs` | Native, variant and marshalling values and builders. |
| `CILParser.SemanticValues.MethodBodies.cs` | Debug, data, security, exception and instruction values. |
| `CILParser.SemanticValues.Signatures.cs` | Managed types, signatures, names, owners and member references. |

These types are public because ANTLR emits public rule-context return and local fields. They are
implementation-only: the explicit reference assembly omits `CILParser`, and its existing CP0001
suppression covers the nested semantic types as part of that excluded surface.

## Rules for grammar actions

Parser actions in `src/ILAssembler/gen/CIL.g4` must remain thin. They pass concrete child-rule
values to `GrammarActions`; mechanical assignments and typed builder additions may happen directly
in the grammar. Compilation orchestration belongs in the `GrammarActions` partial-class files.

`DocumentCompiler` disables parse-tree construction when it creates the parser, and no parser action
changes that setting. ANTLR generates neither listeners nor visitors. All semantics come from parser
actions and concrete synthesized values. Rule contexts own their typed builders and pass finalized
child values to their parents; parser semantic data is never erased to `object`.

All namespace, type-header, top-level, type, signature, reference, marshalling, class-member,
method-header, method-body directive, exception-handling, data, security, source, language,
assembly, manifest, vtable and typedef structure is action-driven. `scopeBlock` records offsets
under its context key without inspecting children. `BuildParseTree` remains disabled throughout.

Rule-local builders are finalized from that rule's `finally` clause when error recovery requires a
value. Semantic roots capture the initial syntax-error count in a context local instead of a global
frame stack. The remaining stacks model active compiler nesting: namespaces, types, declaration
owners and lexical method scopes. Their owning declaration or scope releases them from `finally`
because ANTLR skips `@after` actions after a syntax error.

Only parser actions process structural rules. There is no parse-tree walker or mode toggling.

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
Regeneration produces `CILLexer.cs` and `CILParser.cs`; it does not produce visitor or listener
types.
