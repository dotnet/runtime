# System.Private.CoreLib Shared Sources

This directory contains the shared sources for System.Private.CoreLib library. It represents the majority of the CoreLib implementation.  Each flavor of the runtime (e.g. coreclr, nativeaot, mono) provides additional files as part of their build of CoreLib to complement this directory's contents.

The goal is to have the majority of code located in this folder, as that code is used by both Mono, NativeAot and CoreCLR runtimes. The source code can be shared as a whole file or at the member level by declaring a type as `partial` and having common parts stored here and the rest in runtime-specific location.

### File Naming Convention

Any runtime-specific `partial` part which also has a shared part should use a runtime-specific file name suffix to ease the navigation.

* `*.CoreCLR.cs` for CoreCLR runtime
* `*.NativeAot.cs` for NativeAot runtime
* `*.Mono.cs` for Mono runtime

## System.Private.CoreLib CoreCLR Sources

The CoreCLR specific sources can be found at [src/coreclr/System.Private.CoreLib](/src/coreclr/System.Private.CoreLib/).

## System.Private.CoreLib NativeAot Sources

The NativeAot specific sources can be found at [src/coreclr/nativeaot/System.Private.CoreLib/src](/src/coreclr/nativeaot/System.Private.CoreLib/src/).

## System.Private.CoreLib Mono Sources

The Mono specific sources can be found at [src/mono/System.Private.CoreLib](/src/mono/System.Private.CoreLib/).
