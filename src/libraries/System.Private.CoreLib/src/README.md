# System.Private.CoreLib Shared Sources

This directory contains the shared sources for System.Private.CoreLib library. It represents the majority of the CoreLib implementation.  Each flavor of the runtime (e.g. coreclr, mono) provides additional files as part of their build of CoreLib to complement this directory's contents.

Runtime specific partial part which have shared part use runtime specific suffix to easy the navigation.
    * `.CoreCLR.cs` for CoreCLR runtime
    * `.Mono.cs` for Mono runtime

## System.Private.CoreLib CoreCLR Sources

The CoreCLR specific sources can be found at [src/coreclr/src/System.Private.CoreLib](../../../coreclr/src/System.Private.CoreLib/).

## System.Private.CoreLib Mono Sources

The Mono specific sources can be found at [src/mono/netcore/System.Private.CoreLib](../../../mono/netcore/System.Private.CoreLib/).
