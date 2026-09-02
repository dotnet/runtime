# ReadyToRun Large Version Bubble Features Overview

The ReadyToRun format defaults to the following version bubble behavior: Each assembly is its own version bubble. This means the compiler can only inline methods or make inline observations for methods and types defined in the same assembly or marked as `[NonVersionable]` in CoreLib. This can end up being a serious limitation for pre-compilation of large applications, especially for generic code, where the compiler cannot generate the code for a generic instantiation if the generic type and the generic method are defined in different assemblies. As a result, we have introduced many different "large version bubble" features to enable varying scenarios over time, with various limitations.


## `--inputbubble` and `--inputbubbleref`

These are unsupported options that specify that the current R2R image is part of a larger version bubble (`--inputbubbleref` allows specifying which assemblies are in the bubble). This generates each R2R image in the same format as if no large bubble flags were passed and doesn't do any additional work or accounting to handle assemblies versioning outside the bubble.

## `--opt-cross-module:*` and `--non-local-generics-module`

`--opt-cross-module:` enables cross-module inlining between assemblies, with `-opt-cross-module:*` enabling inlining between all assemblies. This expands the version bubble in a tracked manner, ie. if the module that has code inlined changes, the precompiled code depending on it is invalidated. The `--non-local-generics-module` flag allows a module to be specified as the module to put code for generic instantiations that are now within the "version bubble" of these multiple assemblies, enabling the "cross-module generics" overall feature.

## `--composite`

The composite format is a newer R2R format (see ReadyToRun Composite Format Design) that allows multiple assemblies to be compiled into a single R2R image. This is a more efficient way to represent a large version bubble, as it allows the compiler to inline and generate code across multiple assemblies without having to track dependencies between separate R2R images. The composite format also allows for better sharing of generic instantiations and other code across assemblies in the bubble. However, the composite format comes with some limitations:

All modules that are compiled into the composite format are rewritten to point to the composite R2R image as that's how the composite image is resolved by the runtime. This means that the same assembly on disk cannot be in the same version bubble for multiple composite images.

Today, `--composite` and `--opt-cross-module`/`--non-local-generics-module` are incompatible.
