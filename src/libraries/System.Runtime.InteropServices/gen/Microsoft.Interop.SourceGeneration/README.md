# Shared interop source generation

The shared implementation is split into two libraries, both using the `Microsoft.Interop` namespace:

| Library | Responsibility |
| --- | --- |
| `Microsoft.Interop.SourceGeneration` | Compiler-independent models, marshalling resolvers, diagnostic payloads, text helpers, and stub generation |
| `Microsoft.Interop.SourceGeneration.Roslyn` | Compiler symbol/syntax analysis, marshalling parsers, model factories, diagnostic reporting, and incremental-generator adapters |

The Roslyn library references the core library, never the reverse. Core must build without any Roslyn assembly references. Its models must not retain symbols, syntax trees, compilations, or compiler diagnostic locations. The core project checks resolved references during its build; core-only unit tests also exercise generation and check assembly references.

The Roslyn library exposes compiler-dependent factories and helpers through static extension blocks, preserving calls such as `ManagedTypeInfo.CreateTypeInfoForTypeSymbol(...)`, `TypePositionInfo.CreateForParameter(...)`, and `SignatureContext.Create(...)` without adding compiler dependencies to core. Core owns its ref/scoped enums and the small `SpecialType` set needed for marshalling; conversions must be explicit rather than depending on compiler enum values. Types without special marshalling rules use the general type models.

Core generators report `GeneratorDiagnostic` payloads through `IGeneratorDiagnosticsBag`. The Roslyn implementation, `GeneratorDiagnosticsBag`, adds descriptors and source locations. Additional compiler-specific reporting methods belong on the implementation, not the core interface.

Marshaller type templates cross the boundary as strings; the Roslyn layer computes the templates, and core specializes them. Model types are compiled only in core. Each assembly retains its own internal compiler polyfills and resources, following the generator project conventions, without broad friend access.

Escape compiler-provided identifiers through the Roslyn-provided `CodeWriterHelpers.EscapeIdentifier(string)` extension: keyword classification follows the hosting compiler, whose contextual keywords can change independently of the generator. The core overload accepts that classification as data instead of maintaining a second keyword vocabulary.

Keep emitted text stable when changing this boundary. Generator tests must compare raw source, including line endings, rather than normalize syntax trees. When updating analyzer deployment or packaging, include both shared libraries alongside the generator assembly.
