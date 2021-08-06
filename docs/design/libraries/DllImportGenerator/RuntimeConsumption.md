# Integration into dotnet/runtime

The prototype phase of the `DllImport` source generator is complete. The process and work done was tracked at [dotnet/runtime@43060](https://github.com/dotnet/runtime/issues/43060). The results of the prototype have provided us with some confidence that the current approach is viable and can move forward to consume the source generator in the .NET 7 timeframe. The plan for integration into the dotnet/runtime repository follows.

Converting the prototype into a production ready product is required prior to consumption in the product. This means integration will be done in two phases, production-ready and consumption, that can be done in parallel in many cases. There is a third phase that is considered a stretch goal &ndash; productization.

## Production-ready

The following items must be considered to make this prototype production-ready.

**Move source from dotnet/runtimelab** &ndash; Move the prototype source from its branch in [dotnet/runtimelab](https://github.com/dotnet/runtimelab/tree/feature/DllImportGenerator/DllImportGenerator) to [dotnet/runtime](https://github.com/dotnet/runtime). Guidance on destination can be found at [`project-guidelines.md`](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/project-guidelines.md) &ndash; plan is under `libraries/System.Runtime.InteropServices`. Commit history should be retained during this move.
  - Includes unit and scenario test integration into the `libraries/` pattern.

**Attributes into `System.Private.CoreLib`** &ndash; Prior to productization, triggering and support attributes should be moved into a global space for consumption by `NetCoreApp`.

**Product impact tenets** &ndash; Size impact, security, and convention reviews. Patterns have been uncovered that could be improved upon in the source generation step. Collapsing some of these patterns would help with reducing size impact and addressing potential security issues. Code generation convention changes may result in API proposals for marshal helpers &ndash; see [struct marshalling](./StructMarshalling.md).

**UX** &ndash; Source generators can run from a command line build as well as from the IDE. Running from within the IDE will impact the UX of the IDE and therefore performance investigations are needed to ensure the generator doesn't degrade the developer inner-loop.

**Stakeholder feedback** &ndash; An exit criteria for the prototype was to reach out to stakeholders and get feedback on the experience and generated code. Responses to all feedback prior to product consumption is expected.

## Consumption

The following items must be considered to make the prototype consumable in [dotnet/runtime repository](https://github.com/dotnet/runtime).

**In-box source generation** &ndash; Guidance for [providing an in-box source generator has been documented](https://github.com/dotnet/designs/blob/main/accepted/2021/InboxSourceGenerators.md). The document should be considered a primary source for best-practices that will eventually be needed for productization.

**Versioning/Shipping/Servicing** &ndash; Partially captured in the "In-box source generation" document, but stated item is being called out.

**Merge in feature branch**  &ndash; Integration work for updates to `NetCoreApp` have started in [`feature/use-dllimport-generator`](https://github.com/dotnet/runtime/tree/feature/use-dllimport-generator).
  - Question on the impact of [source build](https://github.com/dotnet/source-build) for dotnet/runtime.

## Productization

The following items must be considered to make this prototype into a product.

**Localization** &ndash; We must ensure user observed messages are properly localized and adhere to the Globalization/Localization tenets.

**API Review** &ndash; [`GeneratedDllImportAttribute`](https://github.com/dotnet/runtime/issues/46822); Supporting [attributes](https://github.com/dotnet/runtime/issues/46838) and marshalling helper types.

**Distribution** &ndash; The consumption of the source generator product is assumed to be in-box and/or as a standalone NuPkg. If not distributed as NuPkg there is no known additional work here.

**Documentation/Sample** &ndash; Productization will require new documentation and at least one official example.
