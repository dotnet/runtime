# System.IO.FileSystem.Primitives
This assembly no longer contains any code. It is provided only to permit type unification for libraries built against previous versions of .NET.

## Source
* All types previously part of this assembly (`FileAccess`, `FileAttributes`, `FileMode`, `FileShare`) are now part of [System.Private.CoreLib](../System.Private.CoreLib/), exposed via [System.Runtime](../System.Runtime/).

* Some tests for types previously in this library are still in the [tests](tests/) subdirectory.

## Contribution Bar
- [x] We consider changes that move tests from the [tests](tests/) subdirectory into [System.IO.Tests](../System.IO/tests/) project.

## Deployment
The System.IO.FileSystem.Primitives assembly is part of the shared framework, and ships with every new release of .NET.
