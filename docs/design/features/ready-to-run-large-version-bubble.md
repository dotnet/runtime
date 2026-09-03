# ReadyToRun Large Version Bubble Features Overview

In ordinary non-composite Crossgen2 compilation, the input assembly forms its own version bubble. The compiler can make version-sensitive observations only about methods and types that version with that bubble, with narrowly constrained exceptions for code marked `[NonVersionable]`. A generic instantiation can be precompiled only when its definition and relevant type arguments satisfy the applicable versioning rules. Canonical types and some primitive, `object`, and `string` instantiations have additional exceptions.

This per-assembly policy can limit optimization and generic precompilation for applications split across many assemblies. Crossgen2 has several mechanisms for relaxing the limitation. They do not all enlarge the version bubble: some create an exact multi-module bubble, while others retain separate versioning and guard individual optimizations with runtime checks.

## `--inputbubble`, `--inputbubbleref`, and `--compilebubblegenerics`

`--inputbubble` declares an exact multi-module version bubble. By default, all references passed to crossgen2 are included in the bubble. If `--inputbubbleref` is specified, only those references are included in the bubble. `--inputbubble` is not a supported or recommended feature.

Each assembly that is passed in as part of the version bubble has R2R code inserted into it, like the standard single-module version-bubble format. Crossgen2 marks the image as having a multi-module version bubble and records the MVIDs of bubble modules used by the image. CoreCLR registers these exact-version dependencies when it loads the image and fail-fasts if an assembly with the same simple name but a different required MVID is loaded.

`--compilebubblegenerics` permits eligible generic instantiations whose definitions are in the declared version bubble to be emitted into the current image, even when their definitions are not in the current input assembly. Such an image is marked as containing R2R code unrelated to its own module so that CoreCLR can consider it during generic entry-point lookup.

Open Questions:

- Why did we decide to not support `--inputbubble`?
- `--inputbubble` is known to have "issues". What are some of these issues? Can we describe them here?

## `--opt-cross-module` and `--non-local-generics-module`

`--opt-cross-module:<assembly>` permits Crossgen2 to inline methods and compile eligible generic instantiations from the specified reference assembly. `--opt-cross-module:*` applies the policy to all eligible reference assemblies. These assemblies deliberately remain outside the version bubble.

For an inline or method body taken from outside the bubble, Crossgen2 emits an IL-body check for the dependent precompiled method. CoreCLR compares the recorded IL and type information with the assembly loaded at runtime. If they do not match, CoreCLR rejects that precompiled method and uses the normal runtime fallback. This is a more resilient contract than `--inputbubble`: it invalidates the code that depends on the changed method rather than imposing an exact MVID requirement on the whole bubble. A module selected by `--opt-cross-module` must be loadable from the time the R2R image is loaded because the generated code can cause it to load unpredictably.

## `--non-local-generics-module:<assembly>`

`--non-local-generics-module:<assembly>` chooses an input module as the home for eligible generic instantiations whose definitions are outside that input. The value is matched case-insensitively as an assembly simple name or a `.dll` filename; `*` uses the first input module as the home and allows it to hold arbitrary eligible generic code. The implementation validates a named value against both inputs and references, but only an input module can be an output home.

This option controls placement, not version-bubble membership or source-module selection. It is used with mechanisms such as `--opt-cross-module` or `--compilebubblegenerics` that make the non-local instantiations eligible for compilation. There is no direct pointer from each defining assembly to the selected home. During generic entry-point lookup, CoreCLR checks the defining module, a deterministic alternate location derived from the generic arguments, and then loaded R2R images marked as containing unrelated generic code.

Without this option, `--opt-cross-module` only enables cross-module inlining, not cross-module generics.

## `--composite`

The [ReadyToRun composite format](readytorun-composite-format-design.md) allows multiple input assemblies to be compiled into one R2R image. All input assemblies are in the same version bubble and compilation unit, so Crossgen2 can optimize across them and share generic instantiations without coordinating code across separate R2R images.

Crossgen2 writes a component copy of each input assembly into the output tree. Each component contains a forwarding R2R header that names one owner composite image, which CoreCLR loads to obtain the component's precompiled code. One physical component file therefore cannot forward to multiple composites. The same original IL assembly can still be compiled into multiple composite images as long as each output has its own generated component copy.

### Composite limitations

The following limitations exist for Composite R2R images at runtime:

When specifying `--non-local-generics-module`, the selected "home" module will not be marked as containing unrelated generic code. Instead, the composite module itself will be marked as such. The code will be emitted into the composite image.

All assemblies in a composite image should be loaded into the same AssemblyLoadContext. If this rule is ignored and an assembly in the bubble was loaded into a different context than the rest of the component modules initially, the copy that is loaded into the same ALC will be jitted and the other copy will use the R2R code.

Open Questions:

- What sort of bugs can we get here? ReJIT invalidation problems? Anything else we can describe here?
- Does this count also for plugin scenarios where the plugin is loaded into a different ALC (and maybe in its own R2R bubble) and the call chain goes from the main application into the plugin and back into the main app?