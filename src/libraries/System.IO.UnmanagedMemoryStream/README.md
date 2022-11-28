# System.IO.UnmanagedMemoryStream
This assembly no longer contains any code.  It is provided only to permit type unification for libraries built against previous versions of .NET.

## Source
* All types previously part of this library are now part of [System.Private.CoreLib](../System.Private.CoreLib/), `UnmanagedMemoryAccessor` is exposed via [System.Runtime.InteropServices](../System.Runtime.InteropServices/) and `UnmanagedMemoryStream` is exposed via [System.Runtime](../System.Runtime/).

* Some of the tests for types previously in this library are still in the [tests](tests/) subdirectory.


## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

## Deployment
The System.IO.UnmanagedMemoryStream assembly is part of the shared framework, and ships with every new release of .NET.
